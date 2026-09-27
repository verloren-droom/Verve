// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>
namespace Verve.Tests.ACC
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using NUnit.Framework;

    /// <summary>
    ///   <para>复制层回归；覆盖分片、背压、恶意输入、自动编码和高压状态变更。</para>
    /// </summary>
    [Category("ACC")]
    public sealed class AccReplicationTests
    {
        private struct Value : IComponent { public int Number; }
        private struct Padded : IComponent { public byte A; public long B; public short C; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Packed : IComponent { public byte A; public long B; public short C; }
        private struct Nested : IComponent { public Padded Item; public float Number; }
        private struct Flag : IComponent { public bool Value; }

        private sealed class FailingCleanup : Capability
        {
            protected override void Dispose(bool disposing)
            {
                if (disposing) throw new IOException("capability cleanup failure");
            }
        }

        private sealed class Quantized : IReplicationCodec<Value>
        {
            public int Size => 2;
            public void Encode(Span<byte> destination, Value value)
                => BinaryPrimitives.WriteInt16LittleEndian(destination, checked((short)value.Number));
            public Value Decode(ReadOnlySpan<byte> source) => new() { Number = BinaryPrimitives.ReadInt16LittleEndian(source) };
        }

        private sealed class Interest : IReplicationInterest
        {
            public bool Visible = true;
            public bool IsRelevant(World world, Actor actor, ReplicationPeer peer) => Visible;
        }

        private sealed class Transport : IReplicationTransport
        {
            internal readonly Queue<byte[]> Incoming = new();
            internal readonly List<byte[]> Sent = new();
            internal int Allowance = int.MaxValue;
            internal int Releases;
            internal bool ThrowOnDispose;
            public bool TrySend(ReadOnlySpan<byte> packet)
            {
                if (Allowance == 0) return false;
                Allowance--;
                Sent.Add(packet.ToArray());
                return true;
            }
            public int Receive(Span<byte> buffer)
            {
                if (Incoming.Count == 0) return 0;
                var packet = Incoming.Dequeue();
                packet.AsSpan().CopyTo(buffer);
                return packet.Length;
            }
            public void Dispose() { Releases++; if (ThrowOnDispose) throw new IOException("dispose failure"); }
        }

        private sealed class Pair : IDisposable
        {
            internal readonly World Authority;
            internal readonly World Replica;
            internal readonly Transport Output = new();
            internal readonly Transport Input = new();
            internal readonly ReplicationPeer Sender;
            internal readonly ReplicationPeer Receiver;

            internal Pair(int packetBytes = 65536, int budget = 64, int maximum = 100000,
                IReplicationInterest interest = null, IReplicationCodec<Value> codec = null)
            {
                var schema = new ReplicationSchema(1).Register(1, codec);
                Authority = new World("authority", replicationOptions: new ReplicationOptions(schema, ReplicationRole.Authority,
                    0, packetBytes, maxActors: maximum, packetsPerTick: budget, interest: interest));
                Replica = new World("replica", replicationOptions: new ReplicationOptions(schema, ReplicationRole.Replica,
                    0, packetBytes, maxActors: maximum, packetsPerTick: budget));
                Sender = Authority.Replication.Attach(Output);
                Receiver = Replica.Replication.Attach(Input);
            }
            internal Actor Add(int value)
            {
                var actor = Authority.CreateActor();
                Authority.AddComponent<Replicated>(actor);
                Authority.AddComponent<Value>(actor).Number = value;
                return actor;
            }
            internal void Deliver()
            {
                foreach (var packet in Output.Sent) Input.Incoming.Enqueue(packet);
                Output.Sent.Clear();
                Replica.Tick(0.01f, TickGroup.Early);
            }
            internal void Step() { Authority.Tick(0.01f, TickGroup.Late); Deliver(); }
            public void Dispose() { Authority.Dispose(); Replica.Dispose(); }
        }

        [Test]
        public void AutomaticCodec_OmitsPaddingAndHasAbiIndependentFormat()
        {
            var padded = new ReplicationCodec<Padded>();
            var packed = new ReplicationCodec<Packed>();
            Assert.That(padded.Size, Is.EqualTo(11));
            Assert.That(padded.Format, Is.EqualTo(packed.Format));
            Assert.That(padded.Format, Is.EqualTo(0xb42fca6b02bcb775UL));
            var bytes = new byte[11];
            padded.Encode(bytes, new Padded { A = 3, B = long.MinValue + 7, C = -42 });
            Assert.That(bytes[0], Is.EqualTo(3));
            Assert.That(BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(1)), Is.EqualTo(long.MinValue + 7));
            var copy = packed.Decode(bytes);
            Assert.That(copy.C, Is.EqualTo(-42));
            Assert.That(copy.B, Is.EqualTo(long.MinValue + 7));
            Assert.Throws<ArgumentException>(() => padded.Decode(bytes.AsSpan(1)));
            Assert.Throws<NotSupportedException>(() => new ReplicationSchema(1).Register<Flag>(1));
            var nested = new ReplicationCodec<Nested>();
            var nestedBytes = new byte[nested.Size];
            nested.Encode(nestedBytes, new Nested { Item = new Padded { B = 98 }, Number = -3.125f });
            Assert.That(nested.Decode(nestedBytes).Item.B, Is.EqualTo(98));
            Assert.That(nested.Decode(nestedBytes).Number, Is.EqualTo(-3.125f));
        }

        [Test]
        public void Schema_IsStableRejectsCollisionsAndFreezes()
        {
            var first = new ReplicationSchema(1).Register<Value>(2).Register<Padded>(1);
            var second = new ReplicationSchema(1).Register<Padded>(1).Register<Value>(2);
            using var one = new World("one", replicationOptions: new ReplicationOptions(first, ReplicationRole.Authority));
            using var two = new World("two", replicationOptions: new ReplicationOptions(second, ReplicationRole.Authority));
            Assert.That(first.Fingerprint, Is.EqualTo(second.Fingerprint));
            Assert.That(first.Fingerprint, Is.EqualTo(0xc49dd0f62ffefd74UL));
            Assert.Throws<InvalidOperationException>(() => first.Register<Nested>(3));
            Assert.Throws<InvalidOperationException>(() => new ReplicationSchema(1).Register<Value>(1).Register<Padded>(1));
            Assert.Throws<InvalidOperationException>(() => new ReplicationSchema(1).Register<Value>(1).Register<Value>(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReplicationOptions(first, ReplicationRole.Authority, float.NaN));
        }

        [Test]
        public void CustomCodec_UsesTheSameLifecycle()
        {
            using var pair = new Pair(codec: new Quantized());
            var actor = pair.Add(-300);
            pair.Step();
            Assert.That(pair.Receiver.TryGetActor(actor.id, out var remote), Is.True);
            Assert.That(pair.Replica.GetComponent<Value>(remote).Number, Is.EqualTo(-300));
        }

        [Test]
        public void FragmentsAndPartialBackpressure_DoNotPublishPartialState()
        {
            using var pair = new Pair(packetBytes: 256, budget: 1);
            var actor = pair.Add(10);
            for (int i = 0; i < 100; i++) pair.Add(i);
            pair.Step();
            Assert.That(pair.Sender.Sequence, Is.Zero);
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.Zero);
            pair.Output.Allowance = 0;
            pair.Authority.GetComponent<Value>(actor).Number = 900;
            pair.Step();
            Assert.That(pair.Sender.Sequence, Is.Zero);
            pair.Output.Allowance = int.MaxValue;
            for (int i = 0; i < 50 && pair.Receiver.Sequence == 0; i++) pair.Step();
            Assert.That(pair.Receiver.TryGetActor(actor.id, out var remote), Is.True);
            Assert.That(pair.Replica.GetComponent<Value>(remote).Number, Is.EqualTo(10));
            for (int i = 0; i < 50 && pair.Receiver.Sequence < 2; i++) pair.Step();
            Assert.That(pair.Replica.GetComponent<Value>(remote).Number, Is.EqualTo(900));
        }

        [TestCase(0), TestCase(4), TestCase(5), TestCase(6), TestCase(8), TestCase(16), TestCase(24), TestCase(32), TestCase(40), TestCase(44)]
        public void MalformedHeader_ClosesOnlyItsConnectionAndKeepsLocalActor(int offset)
        {
            using var pair = new Pair();
            pair.Add(42);
            var local = pair.Replica.CreateActor();
            pair.Replica.AddComponent<Value>(local).Number = 77;
            pair.Authority.Tick(0.01f, TickGroup.Late);
            if (offset == 32 || offset == 40) pair.Output.Sent[0][offset] = 0;
            else pair.Output.Sent[0][offset] ^= 0x7f;
            Assert.Throws<AggregateException>(() => pair.Deliver());
            Assert.That(pair.Receiver.IsClosed, Is.True);
            Assert.That(pair.Input.Releases, Is.EqualTo(1));
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.EqualTo(1));
            Assert.That(pair.Replica.GetComponent<Value>(local).Number, Is.EqualTo(77));
        }

        [TestCase(48), TestCase(68), TestCase(76), TestCase(80)]
        public void MalformedBody_IsDecodedBeforeAnyActorIsCreated(int offset)
        {
            using var pair = new Pair();
            pair.Add(42);
            pair.Authority.Tick(0.01f, TickGroup.Late);
            pair.Output.Sent[0][offset] = 0xff;
            Assert.Throws<AggregateException>(() => pair.Deliver());
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.Zero);
            Assert.That(pair.Receiver.Sequence, Is.Zero);
        }

        [TestCase(false), TestCase(true)]
        public void ReorderedOrRepeatedFragment_IsRejected(bool repeat)
        {
            using var pair = new Pair(packetBytes: 256);
            for (int i = 0; i < 100; i++) pair.Add(i);
            pair.Authority.Tick(0.01f, TickGroup.Late);
            if (repeat) pair.Output.Sent.Insert(1, pair.Output.Sent[0]);
            else (pair.Output.Sent[0], pair.Output.Sent[1]) = (pair.Output.Sent[1], pair.Output.Sent[0]);
            Assert.Throws<AggregateException>(() => pair.Deliver());
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.Zero);
        }

        [Test]
        public void IncompleteBatch_ExpiresWithoutPartialEntities()
        {
            using var pair = new Pair(packetBytes: 256, budget: 1);
            for (int i = 0; i < 100; i++) pair.Add(i);
            pair.Step();
            Assert.Throws<AggregateException>(() => pair.Replica.Tick(11, TickGroup.Early));
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.Zero);
            Assert.That(pair.Input.Releases, Is.EqualTo(1));
        }

        [Test]
        public void InterestLossAndReturn_AutomaticallyReleaseAndRecreateRemoteEntities()
        {
            var interest = new Interest();
            using var pair = new Pair(interest: interest);
            var actor = pair.Add(4);
            pair.Step();
            Assert.That(pair.Receiver.TryGetActor(actor.id, out var first), Is.True);
            Assert.Throws<InvalidOperationException>(() => pair.Replica.DestroyActor(first));
            interest.Visible = false;
            pair.Step();
            Assert.That(pair.Replica.IsActorAlive(first), Is.False);
            interest.Visible = true;
            pair.Step();
            Assert.That(pair.Receiver.TryGetActor(actor.id, out var second), Is.True);
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void ThrowingTransportDisposal_StillReleasesRemoteEntitiesOnce()
        {
            using var pair = new Pair();
            pair.Add(1);
            pair.Step();
            pair.Input.ThrowOnDispose = true;
            Assert.Throws<AggregateException>(() => pair.Replica.Replication.Detach(pair.Receiver));
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.Zero);
            pair.Replica.Replication.Detach(pair.Receiver);
            Assert.That(pair.Input.Releases, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => pair.Replica.Replication.Attach(pair.Input));
            Assert.That(pair.Input.Releases, Is.EqualTo(1));
        }

        [Test]
        public void RemoteCleanup_CallbackFailureStillReleasesEveryOwnedActor()
        {
            using var pair = new Pair();
            var first = pair.Add(1);
            for (int i = 0; i < 128; i++) pair.Add(i);
            pair.Step();
            Assert.That(pair.Receiver.TryGetActor(first.id, out var remote), Is.True);
            pair.Replica.AddCapability<FailingCleanup>(remote);
            var local = pair.Replica.CreateActor();

            Assert.Throws<AggregateException>(() => pair.Replica.Replication.Detach(pair.Receiver));
            Assert.That(pair.Receiver.RemoteActorCount, Is.Zero);
            Assert.That(pair.Replica.Replication.PeerCount, Is.Zero);
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.EqualTo(1));
            Assert.That(pair.Replica.IsActorAlive(local), Is.True);
            Assert.That(pair.Input.Releases, Is.EqualTo(1));
        }

        [Test]
        public void MultiplePeers_HaveIndependentBaselines()
        {
            using var pair = new Pair();
            var actor = pair.Add(1);
            pair.Step();
            var slow = new Transport { Allowance = 0 };
            var peer = pair.Authority.Replication.Attach(slow);
            pair.Authority.GetComponent<Value>(actor).Number = 5;
            pair.Step();
            Assert.That(pair.Sender.Sequence, Is.EqualTo(2));
            Assert.That(peer.Sequence, Is.Zero);
            slow.Allowance = int.MaxValue;
            pair.Step();
            Assert.That(peer.Sequence, Is.EqualTo(1));
            Assert.That(ReplicationHeader.Read(slow.Sent[0], 8388608).Full, Is.True);
        }

        [TestCase(10000), TestCase(100000)]
        public void HighVolumeMathAndChurn_ReplicateWithoutLoss(int count)
        {
            using var pair = new Pair();
            var actors = new Actor[count];
            for (int i = 0; i < count; i++) actors[i] = pair.Add(i);
            pair.Step();
            Assert.That(pair.Receiver.RemoteActorCount, Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                if (i % 5 == 0) pair.Authority.DestroyActor(actors[i]);
                else
                {
                    double value = i;
                    for (int k = 0; k < 12; k++) value = Math.Sin(value) + Math.Cos(value * 0.31) + Math.Sqrt(Math.Abs(value) + 1);
                    pair.Authority.GetComponent<Value>(actors[i]).Number = (int)(value * 10000);
                }
            }
            pair.Step();
            for (int i = 0; i < count; i++)
            {
                if (i % 5 == 0) Assert.That(pair.Receiver.TryGetActor(actors[i].id, out _), Is.False);
                else
                {
                    Assert.That(pair.Receiver.TryGetActor(actors[i].id, out var remote), Is.True);
                    Assert.That(pair.Replica.GetComponent<Value>(remote).Number, Is.EqualTo(pair.Authority.GetComponent<Value>(actors[i]).Number));
                }
            }
            pair.Replica.Replication.Detach(pair.Receiver);
            Assert.That(pair.Replica.Actors.AliveActorCount, Is.Zero);
        }
    }
}
