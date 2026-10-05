// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>时间记录器的数据视图；轨道按行动者生命周期和能力类型区分。</para>
    /// </summary>
    internal sealed class ACCTemporalTimeline
    {
        internal const int AllKinds = (1 << 6) - 1;

        internal sealed class Track
        {
            internal readonly Actor actor;
            internal readonly Type capabilityType;
            internal readonly List<int> events = new();
            internal readonly List<int> visibleEvents = new();
            internal readonly List<Span> spans = new();
            internal int row = -1;

            internal Track(Actor actor, Type capabilityType)
            {
                this.actor = actor;
                this.capabilityType = capabilityType;
            }

            internal string Name => capabilityType?.Name ?? "未知能力";
        }

        /// <summary>
        ///   <para>已观测到的激活区间；缺失激活事件时不推断缓冲区之前的状态。</para>
        /// </summary>
        internal readonly struct Span
        {
            internal readonly int start;
            internal readonly int end;

            internal Span(int start, int end)
            {
                this.start = start;
                this.end = end;
            }
        }

        internal sealed class Group
        {
            internal readonly Actor actor;
            internal readonly List<Track> tracks = new();

            internal Group(Actor actor) => this.actor = actor;
        }

        internal readonly struct Row
        {
            internal readonly Group group;
            internal readonly Track track;

            internal Row(Group group, Track track = null)
            {
                this.group = group;
                this.track = track;
            }
        }

        internal readonly List<CapabilityDebugEvent> events = new(256);
        internal readonly List<Group> groups = new();
        internal readonly List<Row> rows = new();
        internal readonly List<int> visibleEvents = new(256);
        private readonly Dictionary<(Actor, Type), Track> m_Tracks = new();
        private readonly Dictionary<Actor, Group> m_Groups = new();

        /// <summary>
        ///   <para>替换快照；区间构造始终使用未过滤事件，防止筛选隐藏停用事件后出现假激活。</para>
        /// </summary>
        internal void SetEvents(List<CapabilityDebugEvent> source)
        {
            events.Clear();
            events.AddRange(source);
            groups.Clear();
            m_Tracks.Clear();
            m_Groups.Clear();
            for (var i = 0; i < events.Count; i++)
            {
                var item = events[i];
                var key = (item.actor, item.capabilityType);
                if (!m_Tracks.TryGetValue(key, out var track))
                {
                    track = new Track(item.actor, item.capabilityType);
                    m_Tracks.Add(key, track);
                    if (!m_Groups.TryGetValue(item.actor, out var group))
                    {
                        group = new Group(item.actor);
                        m_Groups.Add(item.actor, group);
                        groups.Add(group);
                    }
                    group.tracks.Add(track);
                }
                track.events.Add(i);
            }
            groups.Sort((left, right) => left.actor.CompareTo(right.actor));
            foreach (var group in groups)
            {
                group.tracks.Sort((left, right) => string.Compare(left.capabilityType?.FullName,
                    right.capabilityType?.FullName, StringComparison.Ordinal));
                foreach (var track in group.tracks)
                    BuildSpans(track);
            }
        }

        private void BuildSpans(Track track)
        {
            var start = -1;
            foreach (var index in track.events)
            {
                switch (events[index].kind)
                {
                    case CapabilityDebugEventKind.Activated:
                        if (start < 0) start = index;
                        break;
                    case CapabilityDebugEventKind.Deactivated:
                    case CapabilityDebugEventKind.Removed:
                    case CapabilityDebugEventKind.Added:
                        if (start >= 0) track.spans.Add(new Span(start, index));
                        start = -1;
                        break;
                }
            }
            if (start >= 0) track.spans.Add(new Span(start, -1));
        }

        /// <summary>
        ///   <para>重建可见行；折叠只影响行布局，不影响事件导航和分组摘要。</para>
        /// </summary>
        internal void Filter(string search, int kinds, HashSet<Actor> collapsed)
        {
            search = search?.Trim() ?? string.Empty;
            rows.Clear();
            visibleEvents.Clear();
            foreach (var group in groups)
            {
                var groupRow = -1;
                foreach (var track in group.tracks)
                {
                    track.row = -1;
                    track.visibleEvents.Clear();
                    var matchesTrack = Matches(track.capabilityType?.FullName, search) ||
                        Matches(track.actor.ToString(), search);
                    foreach (var index in track.events)
                    {
                        var item = events[index];
                        if ((kinds & (1 << (int)item.kind)) == 0 ||
                            (!matchesTrack && !Matches(item.error, search)))
                            continue;
                        track.visibleEvents.Add(index);
                        visibleEvents.Add(index);
                    }
                    if (track.visibleEvents.Count == 0) continue;
                    if (groupRow < 0)
                    {
                        groupRow = rows.Count;
                        rows.Add(new Row(group));
                    }
                    if (collapsed.Contains(group.actor)) continue;
                    track.row = rows.Count;
                    rows.Add(new Row(group, track));
                }
            }
            visibleEvents.Sort();
        }

        internal Track GetTrack(CapabilityDebugEvent item)
            => m_Tracks.TryGetValue((item.actor, item.capabilityType), out var track) ? track : null;

        internal int FindSequence(long sequence)
        {
            for (var i = 0; i < events.Count; i++)
                if (events[i].sequence == sequence) return i;
            return -1;
        }

        /// <summary>
        ///   <para>仅在指定行和像素容差内拾取；重复点击同位置可轮换同帧事件。</para>
        /// </summary>
        internal int HitTest(int row, double time, double tolerance, long afterSequence = -1)
        {
            if (row < 0 || row >= rows.Count) return -1;
            var data = rows[row];
            var nearest = -1;
            var next = -1;
            var distance = double.MaxValue;
            foreach (var index in visibleEvents)
            {
                var item = events[index];
                if (item.actor != data.group.actor ||
                    (data.track != null && item.capabilityType != data.track.capabilityType)) continue;
                var candidate = Math.Abs(item.time - time);
                if (candidate > tolerance) continue;
                if (candidate + 1e-8 < distance)
                {
                    distance = candidate;
                    nearest = index;
                    next = -1;
                }
                if (Math.Abs(candidate - distance) < 1e-8 && item.sequence > afterSequence && next < 0)
                    next = index;
            }
            return next >= 0 ? next : nearest;
        }

        /// <summary>
        ///   <para>实时跟随保留右侧余量，并跳过已被滚动缓冲淘汰的时间段。</para>
        /// </summary>
        internal static double FollowViewStart(double start, double end, double viewStart, double duration)
        {
            viewStart = Math.Max(start, viewStart);
            return end < viewStart || end > viewStart + duration * 0.9d
                ? Math.Max(start, end - duration * 0.9d) : viewStart;
        }

        internal static double TickInterval(double duration, double width, double labelSpacing = 80d)
        {
            var target = Math.Max(duration / Math.Max(width / labelSpacing, 1d), 0.0001d);
            var power = Math.Pow(10d, Math.Floor(Math.Log10(target)));
            var normalized = target / power;
            return (normalized > 5d ? 10d : normalized > 2d ? 5d : normalized > 1d ? 2d : 1d) * power;
        }

        private static bool Matches(string text, string search)
            => search.Length == 0 || (!string.IsNullOrEmpty(text) &&
                text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

#endif
