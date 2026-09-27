//! ACC v2 replication framing; all multi-byte values are little endian.

use std::convert::TryInto;

/// Fixed v2 header size.
pub const HEADER_SIZE: usize = 48;
/// Wire protocol version.
pub const PROTOCOL_VERSION: u8 = 2;
/// Full state batch.
pub const FULL_KIND: u8 = 1;
/// Delta state batch.
pub const DELTA_KIND: u8 = 2;
/// Little-endian `VACC` marker.
pub const MAGIC: u32 = 0x4343_4156;

/// A validated ACC v2 fragment header.
#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub struct PacketHeader {
    /// Full state when true; delta state otherwise.
    pub full: bool,
    /// Replication schema fingerprint.
    pub schema: u64,
    /// Monotonic batch sequence.
    pub sequence: u64,
    /// Sequence required as the receiver baseline.
    pub baseline: u64,
    /// Authoritative simulation tick.
    pub tick: u64,
    /// Complete batch payload length, excluding this header.
    pub total: u32,
    /// Offset of this fragment within the complete payload.
    pub offset: u32,
}

/// ACC v2 header validation failure.
#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub enum ProtocolError {
    /// Header or frame is too short.
    TooShort,
    /// Frame or batch exceeds the configured limit.
    PacketTooLarge,
    /// Wire marker is invalid.
    WrongMagic,
    /// Protocol version is unsupported.
    WrongVersion,
    /// Fragment kind or reserved flags are invalid.
    WrongKind,
    /// Sequence and baseline are inconsistent.
    InvalidSequence,
    /// Schema, sequence, or tick is zero.
    EmptyIdentity,
    /// Batch length or fragment range is invalid.
    InvalidFragment,
}

impl std::fmt::Display for ProtocolError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "invalid ACC v2 packet: {self:?}")
    }
}

impl std::error::Error for ProtocolError {}

impl PacketHeader {
    /// Validate and decode one complete transport frame.
    pub fn decode(packet: &[u8], max_batch_size: usize) -> Result<Self, ProtocolError> {
        if packet.len() <= HEADER_SIZE {
            return Err(ProtocolError::TooShort);
        }
        if packet.len() > max_batch_size.saturating_add(HEADER_SIZE) {
            return Err(ProtocolError::PacketTooLarge);
        }
        let marker = u32::from_le_bytes(packet[0..4].try_into().expect("validated header size"));
        if marker != MAGIC {
            return Err(ProtocolError::WrongMagic);
        }
        if packet[4] != PROTOCOL_VERSION {
            return Err(ProtocolError::WrongVersion);
        }
        let kind = packet[5];
        let flags = u16::from_le_bytes(packet[6..8].try_into().expect("validated header size"));
        if (kind != FULL_KIND && kind != DELTA_KIND) || flags != 0 {
            return Err(ProtocolError::WrongKind);
        }
        let header = Self {
            full: kind == FULL_KIND,
            schema: u64::from_le_bytes(packet[8..16].try_into().expect("validated header size")),
            sequence: u64::from_le_bytes(packet[16..24].try_into().expect("validated header size")),
            baseline: u64::from_le_bytes(packet[24..32].try_into().expect("validated header size")),
            tick: u64::from_le_bytes(packet[32..40].try_into().expect("validated header size")),
            total: u32::from_le_bytes(packet[40..44].try_into().expect("validated header size")),
            offset: u32::from_le_bytes(packet[44..48].try_into().expect("validated header size")),
        };
        if header.schema == 0 || header.sequence == 0 || header.tick == 0 {
            return Err(ProtocolError::EmptyIdentity);
        }
        if header.baseline == u64::MAX
            || header.sequence != header.baseline.saturating_add(1)
            || header.full != (header.baseline == 0)
        {
            return Err(ProtocolError::InvalidSequence);
        }
        let total = header.total as usize;
        let offset = header.offset as usize;
        if total < 12 || total > max_batch_size || offset >= total {
            return Err(ProtocolError::InvalidFragment);
        }
        if packet.len() - HEADER_SIZE > total - offset {
            return Err(ProtocolError::InvalidFragment);
        }
        Ok(header)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn packet() -> Vec<u8> {
        let mut bytes = vec![0; 60];
        bytes[0..4].copy_from_slice(&MAGIC.to_le_bytes());
        bytes[4] = PROTOCOL_VERSION;
        bytes[5] = FULL_KIND;
        bytes[8..16].copy_from_slice(&7u64.to_le_bytes());
        bytes[16..24].copy_from_slice(&1u64.to_le_bytes());
        bytes[32..40].copy_from_slice(&1u64.to_le_bytes());
        bytes[40..44].copy_from_slice(&12u32.to_le_bytes());
        bytes
    }

    #[test]
    fn full_and_delta_ranges_match_csharp() {
        let mut bytes = packet();
        assert!(PacketHeader::decode(&bytes, 12).unwrap().full);
        bytes[5] = DELTA_KIND;
        bytes[16..24].copy_from_slice(&2u64.to_le_bytes());
        bytes[24..32].copy_from_slice(&1u64.to_le_bytes());
        assert!(!PacketHeader::decode(&bytes, 12).unwrap().full);
        bytes[44..48].copy_from_slice(&1u32.to_le_bytes());
        assert_eq!(
            PacketHeader::decode(&bytes, 12),
            Err(ProtocolError::InvalidFragment)
        );
        bytes.pop();
        assert!(PacketHeader::decode(&bytes, 12).is_ok());
    }

    #[test]
    fn invalid_headers_and_truncation_are_rejected() {
        let good = packet();
        for length in 0..=HEADER_SIZE {
            assert!(PacketHeader::decode(&good[..length], 1024).is_err());
        }
        for (offset, value) in [
            (0, 0),
            (4, 1),
            (5, 3),
            (6, 1),
            (8, 0),
            (16, 0),
            (24, 1),
            (32, 0),
            (40, 11),
            (44, 12),
        ] {
            let mut bad = good.clone();
            bad[offset] = value;
            assert!(
                PacketHeader::decode(&bad, 1024).is_err(),
                "accepted invalid byte {offset}"
            );
        }
        assert!(PacketHeader::decode(&good, 11).is_err());
    }

    #[test]
    fn sequence_overflow_never_wraps() {
        let mut bytes = packet();
        bytes[5] = DELTA_KIND;
        bytes[16..24].copy_from_slice(&u64::MAX.to_le_bytes());
        bytes[24..32].copy_from_slice(&(u64::MAX - 1).to_le_bytes());
        assert!(PacketHeader::decode(&bytes, 12).is_ok());
        bytes[24..32].copy_from_slice(&u64::MAX.to_le_bytes());
        assert_eq!(
            PacketHeader::decode(&bytes, 12),
            Err(ProtocolError::InvalidSequence)
        );
    }
}
