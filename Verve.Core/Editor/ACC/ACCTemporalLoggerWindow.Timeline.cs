// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Globalization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal sealed partial class ACCTemporalLoggerWindow
    {
        // 采用动画编辑器的中性轨道、细分隔线与蓝色播放头；颜色随编辑器主题切换。
        private Color WindowColor => Gray(m_DarkSkin ? 0.16f : 0.76f);
        private Color HeaderColor => Gray(m_DarkSkin ? 0.235f : 0.82f);
        private Color RowColor => Gray(m_DarkSkin ? 0.19f : 0.88f);
        private Color AlternateRowColor => Gray(m_DarkSkin ? 0.205f : 0.855f);
        private Color GroupColor => Gray(m_DarkSkin ? 0.265f : 0.77f);
        private Color BorderColor => Gray(m_DarkSkin ? 0.12f : 0.60f);
        private Color GridColor => new Color(0f, 0f, 0f, m_DarkSkin ? 0.32f : 0.16f);
        private Color TextColor => Gray(m_DarkSkin ? 0.82f : 0.12f);
        private Color MutedColor => Gray(m_DarkSkin ? 0.58f : 0.38f);
        private Color SelectionColor => m_DarkSkin ? new Color(0.18f, 0.32f, 0.48f) : new Color(0.52f, 0.69f, 0.86f);
        private static Color PlayheadColor => new Color(0.25f, 0.62f, 0.94f);

        private static Color Gray(float value) => new Color(value, value, value);

        private static VisualElement CreateHorizontal()
        {
            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.style.minWidth = 0f;
            return element;
        }

        private static ToolbarToggle CreateIconToggle(string iconName, string tooltip, bool value, Action<bool> changed)
        {
            var toggle = new ToolbarToggle { tooltip = tooltip };
            toggle.SetValueWithoutNotify(value);
            toggle.RegisterValueChangedCallback(evt => changed(evt.newValue));
            toggle.style.width = 26f;
            // ToolbarToggle 的默认复选框容器不参与图标布局。
            var input = toggle.Q(className: "unity-toggle__input");
            if (input != null) input.style.display = DisplayStyle.None;
            toggle.Add(CreateToolbarIcon(iconName));
            return toggle;
        }

        private static Image CreateToolbarIcon(string iconName)
        {
            var icon = new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            icon.style.width = icon.style.minWidth = icon.style.maxWidth = IconSize;
            icon.style.height = icon.style.minHeight = icon.style.maxHeight = IconSize;
            icon.style.flexShrink = 0f;
            icon.style.alignSelf = Align.Center;
            icon.style.marginLeft = icon.style.marginRight = 0f;
            icon.style.marginTop = icon.style.marginBottom = 0f;
            return icon;
        }

        /// <summary>
        ///   <para>统一混合编辑器控件的盒模型；不依赖各控件默认的行高和外边距。</para>
        /// </summary>
        private void NormalizeToolbar(Toolbar toolbar)
        {
            toolbar.style.height = toolbar.style.minHeight = toolbar.style.maxHeight = ToolbarHeight;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.marginTop = toolbar.style.marginBottom = 0f;
            toolbar.style.paddingLeft = toolbar.style.paddingRight = 0f;
            toolbar.style.paddingTop = toolbar.style.paddingBottom = 0f;
            toolbar.style.borderTopWidth = 0f;
            toolbar.style.borderBottomWidth = 1f;
            toolbar.style.borderBottomColor = BorderColor;
            foreach (var control in toolbar.Children())
            {
                if (control is ToolbarSpacer) continue;
                NormalizeControl(control);
                if (control is ToolbarToggle || control is Button || control is ToolbarMenu)
                    control.style.borderLeftWidth = 0f;
            }
        }

        private static void NormalizeControl(VisualElement control)
        {
            control.style.height = control.style.minHeight = control.style.maxHeight = ControlHeight;
            control.style.alignSelf = Align.Center;
            control.style.marginTop = control.style.marginBottom = 0f;
            control.style.marginLeft = control.style.marginRight = 0f;
            control.style.paddingTop = control.style.paddingBottom = 0f;
            control.style.fontSize = 11f;
            control.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (control is ToolbarToggle || control is Button || control is ToolbarMenu)
            {
                control.style.flexDirection = FlexDirection.Row;
                control.style.alignItems = Align.Center;
                control.style.justifyContent = Justify.Center;
                control.style.paddingLeft = control.style.paddingRight = 5f;
                control.style.flexShrink = 0f;
                control.style.minWidth = 24f;
            }
            if (control is Label)
            {
                control.style.minWidth = 12f;
                control.style.paddingLeft = control.style.paddingRight = 2f;
            }
            if (control is DoubleField || control is DropdownField)
            {
                control.style.paddingLeft = control.style.paddingRight = 0f;
                var input = control.Q(className: "unity-base-field__input");
                if (input != null)
                {
                    input.style.minHeight = 0f;
                    input.style.height = ControlHeight - 2f;
                    input.style.alignSelf = Align.Center;
                    input.style.marginTop = input.style.marginBottom = 0f;
                    input.style.paddingTop = input.style.paddingBottom = 0f;
                    input.style.unityTextAlign = TextAnchor.MiddleLeft;
                }
            }
            if (control is ToolbarMenu menu)
            {
                var text = menu.Q<TextElement>(className: ToolbarMenu.textUssClassName);
                text.style.height = ControlHeight;
                text.style.alignSelf = Align.Center;
                var arrow = menu.Q(className: ToolbarMenu.arrowUssClassName);
                arrow.style.alignSelf = Align.Center;
                arrow.style.marginTop = arrow.style.marginBottom = 0f;
            }
            if (control is ToolbarSearchField)
            {
                control.style.flexDirection = FlexDirection.Row;
                control.style.alignItems = Align.Center;
                control.style.paddingLeft = control.style.paddingRight = 2f;
                foreach (var child in control.Children())
                {
                    child.style.position = Position.Relative;
                    child.style.left = child.style.right = child.style.top = child.style.bottom = StyleKeyword.Auto;
                    child.style.marginTop = child.style.marginBottom = 0f;
                    child.style.alignSelf = Align.Center;
                    if (child is Button)
                    {
                        // 搜索与清除按钮沿用 Unity 原生的小图标尺寸。
                        child.style.flexShrink = 0f;
                    }
                    else
                    {
                        child.style.marginLeft = child.style.marginRight = 0f;
                        child.style.paddingTop = child.style.paddingBottom = 0f;
                        child.style.height = ControlHeight - 2f;
                        child.style.flexGrow = 1f;
                        child.style.minWidth = 0f;
                    }
                }
                var input = control.Q(className: "unity-base-field__input");
                if (input != null)
                {
                    input.style.height = ControlHeight - 2f;
                    input.style.minHeight = 0f;
                    input.style.paddingTop = input.style.paddingBottom = 0f;
                }
            }
            control.Query<TextElement>().ForEach(text =>
            {
                text.style.unityTextAlign = TextAnchor.MiddleCenter;
                text.style.marginTop = text.style.marginBottom = 0f;
                text.style.paddingTop = text.style.paddingBottom = 0f;
                if (control is DropdownField || control is DoubleField || control is ToolbarSearchField)
                    text.style.unityTextAlign = TextAnchor.MiddleLeft;
            });
        }

        private static ToolbarButton CreateIconButton(string iconName, string tooltip, Action action)
        {
            var button = new ToolbarButton(action) { tooltip = tooltip };
            button.style.width = 26f;
            button.style.flexShrink = 0f;
            button.style.paddingLeft = button.style.paddingRight = 4f;
            button.Add(CreateToolbarIcon(iconName));
            return button;
        }

        private static ToolbarButton CreateTextButton(string name, string text, Action action)
        {
            // 独立的文字行不参与按钮按下、焦点等状态的盒模型变化。
            var button = new ToolbarButton(action) { name = name };
            button.style.borderTopWidth = button.style.borderBottomWidth = 0f;
            var label = new Label(text) { name = name + "-text", pickingMode = PickingMode.Ignore };
            label.style.height = label.style.minHeight = label.style.maxHeight = ControlHeight;
            label.style.flexGrow = 1f;
            label.style.minWidth = 0f;
            label.style.alignSelf = Align.Center;
            label.style.marginLeft = label.style.marginRight = label.style.marginTop = label.style.marginBottom = 0f;
            label.style.paddingLeft = label.style.paddingRight = label.style.paddingTop = label.style.paddingBottom = 0f;
            label.style.fontSize = 11f;
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            button.Add(label);
            return button;
        }

        /// <summary>
        ///   <para>重建 Actor 分组及能力表头；布局高度与绘制、拾取共用同一行模型。</para>
        /// </summary>
        private void RebuildRows()
        {
            if (m_TrackRows == null) return;
            m_Timeline.Filter(m_Search, m_KindMask, m_CollapsedActors);
            m_TrackRows.Clear();
            m_RowHeaders.Clear();
            foreach (var data in m_Timeline.rows)
            {
                var row = CreateHorizontal();
                row.style.height = RowHeight;
                row.style.flexShrink = 0f;
                row.style.alignItems = Align.Center;
                row.style.borderBottomWidth = 1f;
                row.style.borderBottomColor = BorderColor;
                row.style.overflow = Overflow.Hidden;
                var track = data.track;
                if (track == null)
                {
                    var foldout = new Toggle { value = !m_CollapsedActors.Contains(data.group.actor) };
                    foldout.AddToClassList(Foldout.toggleUssClassName);
                    foldout.style.width = 16f;
                    foldout.style.marginLeft = 4f;
                    foldout.style.marginRight = 2f;
                    var actor = data.group.actor;
                    foldout.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue) m_CollapsedActors.Remove(actor);
                        else m_CollapsedActors.Add(actor);
                        RebuildRows();
                    });
                    row.Add(foldout);
                    var title = CreateRowLabel(data.group.actor.ToString());
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    row.Add(title);
                    row.tooltip = "Actor 分组；摘要菱形汇总所有匹配事件";
                    row.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button != 0 || evt.target is VisualElement target &&
                            (target == foldout || foldout.Contains(target))) return;
                        SelectActor(actor);
                        evt.StopPropagation();
                    });
                }
                else
                {
                    var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                    swatch.style.width = 3f;
                    swatch.style.height = RowHeight - 8f;
                    swatch.style.marginLeft = 23f;
                    swatch.style.marginRight = 7f;
                    swatch.style.backgroundColor = GetEventColor(CapabilityDebugEventKind.Activated);
                    row.Add(swatch);
                    row.Add(CreateRowLabel(track.Name));
                    var count = new Label(track.visibleEvents.Count.ToString()) { pickingMode = PickingMode.Ignore };
                    count.style.color = MutedColor;
                    count.style.fontSize = 10f;
                    count.style.marginRight = 8f;
                    row.Add(count);
                    row.tooltip = $"{track.actor}\n{track.capabilityType?.FullName}\n{track.events.Count} 个已保留事件";
                    row.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button != 0) return;
                        m_SelectedTrack = track;
                        m_SelectedSequence = -1;
                        m_SelectedActor = track.actor;
                        m_Canvas.Focus();
                        UpdateSelection();
                        UpdateComponents();
                    });
                }
                m_RowHeaders.Add(row);
                m_TrackRows.Add(row);
            }
            UpdateTrackHeight();
            UpdateSelection();
            UpdateComponents();
            UpdateStatus();
        }

        private static Label CreateRowLabel(string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.flexGrow = 1f;
            label.style.minWidth = 0f;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        private void UpdateTrackHeight()
        {
            if (m_Canvas == null || m_TrackScroll == null) return;
            var viewportHeight = m_TrackScroll.contentViewport.resolvedStyle.height;
            if (!Game.NumberUtility.IsFinite(viewportHeight)) viewportHeight = 0f;
            var height = Mathf.Max(viewportHeight, m_Timeline.rows.Count * RowHeight);
            m_Canvas.style.height = height;
            m_TrackRows.style.minHeight = height;
            m_Canvas.MarkDirtyRepaint();
        }

        private void UpdateSelection()
        {
            for (var i = 0; i < m_RowHeaders.Count; i++)
            {
                var row = m_Timeline.rows[i];
                m_RowHeaders[i].style.backgroundColor = (row.track != null && ReferenceEquals(row.track, m_SelectedTrack)) ||
                    (row.track == null && row.group.actor == m_SelectedActor)
                    ? SelectionColor : row.track == null ? GroupColor : HeaderColor;
            }
            m_Canvas.MarkDirtyRepaint();
        }

        private void SetTrackWidth(float width)
        {
            m_TrackWidth = Mathf.Clamp(width, 180f, Mathf.Max(180f, rootVisualElement.resolvedStyle.width - 320f));
            m_TrackHeader.style.width = m_TrackWidth;
            m_WorldField.style.width = m_TrackWidth;
            m_TrackRows.style.width = m_TrackWidth;
            m_SearchField.style.width = m_TrackWidth;
            m_StatusLabel.style.width = m_TrackWidth;
            m_Splitter.style.left = m_TrackWidth - 2f;
            m_EmptyLabel.style.left = m_TrackWidth;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (!m_Initialized) return;
            SetTrackWidth(m_TrackWidth);
            UpdateTrackHeight();
        }

        /// <summary>
        ///   <para>更新视口投影；缩放不改变采样数据，也不生成超宽画布。</para>
        /// </summary>
        private void UpdateView()
        {
            m_PlayToggle?.SetValueWithoutNotify(IsPlaybackActive);
            if (m_PlayIcon != null)
                m_PlayIcon.image = EditorGUIUtility.IconContent(IsPlaybackActive ? "PauseButton" : "Animation.Play").image;
            if (!m_Initialized || m_Ruler == null) return;
            RebuildRuler();
            m_TimeField.SetValueWithoutNotify(m_Playhead - TimeOffset);
            var extent = Math.Max(m_DataEnd, m_ViewStart + m_ViewDuration) - m_DataStart;
            var scrollable = Math.Max(0d, extent - m_ViewDuration);
            m_UpdatingScroller = true;
            m_TimeScroller.Adjust((float)Math.Min(1d, m_ViewDuration / Math.Max(extent, MinDuration)));
            m_TimeScroller.value = scrollable > 0d ? (float)((m_ViewStart - m_DataStart) / scrollable) : 0f;
            m_UpdatingScroller = false;
            m_Ruler.MarkDirtyRepaint();
            m_Canvas.MarkDirtyRepaint();
        }

        private float TimeToPixel(double time) => (float)((time - m_ViewStart) / m_ViewDuration * CanvasWidth);
        private double PixelToTime(float pixel) => m_ViewStart + pixel / CanvasWidth * m_ViewDuration;

        private double GetMajorTickInterval()
        {
            var largest = Math.Max(Math.Abs(m_ViewStart - TimeOffset), Math.Abs(m_ViewStart + m_ViewDuration - TimeOffset));
            var labelWidth = largest.ToString("0.####", CultureInfo.InvariantCulture).Length * 7d + 16d;
            return ACCTemporalTimeline.TickInterval(m_ViewDuration, CanvasWidth, Math.Max(72d, labelWidth));
        }

        private void RebuildRuler()
        {
            var interval = GetMajorTickInterval();
            var first = Math.Ceiling((m_ViewStart - TimeOffset) / interval);
            var count = Math.Min(256, (int)Math.Ceiling(m_ViewDuration / interval) + 1);
            while (m_RulerLabels.Count < count)
            {
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.style.position = Position.Absolute;
                label.style.top = 2f;
                label.style.fontSize = 10f;
                label.style.color = MutedColor;
                m_RulerLabels.Add(label);
                m_Ruler.Add(label);
            }
            var decimals = Math.Max(0, Math.Min(4, -(int)Math.Floor(Math.Log10(interval))));
            for (var i = 0; i < m_RulerLabels.Count; i++)
            {
                var label = m_RulerLabels[i];
                label.style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
                if (i >= count) continue;
                var time = (first + i) * interval;
                label.text = time.ToString("F" + decimals);
                label.style.left = TimeToPixel(time + TimeOffset) + 3f;
            }
            if (m_TimeBadge == null || m_TimeBadge.parent != m_Ruler)
            {
                m_TimeBadge = new Label { pickingMode = PickingMode.Ignore };
                m_TimeBadge.style.position = Position.Absolute;
                m_TimeBadge.style.top = 0f;
                m_TimeBadge.style.fontSize = 10f;
                m_TimeBadge.style.paddingLeft = m_TimeBadge.style.paddingRight = 4f;
                m_TimeBadge.style.backgroundColor = PlayheadColor;
                m_TimeBadge.style.color = Color.white;
                m_Ruler.Add(m_TimeBadge);
            }
            m_TimeBadge.BringToFront();
            m_TimeBadge.text = (m_Playhead - TimeOffset).ToString("0.000") + " s";
            m_TimeBadge.style.left = Mathf.Clamp(TimeToPixel(m_Playhead) + 6f, 0f, Mathf.Max(0f, CanvasWidth - 85f));
            m_TimeBadge.style.display = m_DragMode == DragMode.Scrub ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void PaintRuler(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var major = GetMajorTickInterval();
            var minor = major / 5d;
            var first = Math.Ceiling((m_ViewStart - TimeOffset) / minor);
            var count = Math.Min(2048, (int)Math.Ceiling(m_ViewDuration / minor) + 1);
            for (var i = 0; i < count; i++)
            {
                var tick = first + i;
                var x = TimeToPixel(tick * minor + TimeOffset);
                var isMajor = Math.Abs(tick % 5d) < 0.01d;
                var color = GridColor;
                if (!isMajor) color.a *= 0.6f;
                Line(painter, new Vector2(x, isMajor ? 6f : 23f), new Vector2(x, RulerHeight), color);
            }
            Line(painter, new Vector2(0f, RulerHeight - 1f), new Vector2(CanvasWidth, RulerHeight - 1f), BorderColor);
            var playheadX = TimeToPixel(m_Playhead);
            if (playheadX < 0f || playheadX > CanvasWidth) return;
            painter.fillColor = PlayheadColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(playheadX - 5f, RulerHeight - 12f));
            painter.LineTo(new Vector2(playheadX + 5f, RulerHeight - 12f));
            painter.LineTo(new Vector2(playheadX + 5f, RulerHeight - 6f));
            painter.LineTo(new Vector2(playheadX, RulerHeight));
            painter.LineTo(new Vector2(playheadX - 5f, RulerHeight - 6f));
            painter.ClosePath();
            painter.Fill();
        }

        private void PaintCanvas(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var width = CanvasWidth;
            var height = m_Canvas.resolvedStyle.height;
            if (!Game.NumberUtility.IsFinite(height) || height <= 0f) return;
            FillRect(painter, new Rect(0f, 0f, width, height), WindowColor);
            var top = m_TrackScroll.scrollOffset.y;
            var bottom = Mathf.Min(height, top + m_TrackScroll.contentViewport.resolvedStyle.height);
            if (!Game.NumberUtility.IsFinite(bottom)) bottom = height;
            var firstRow = Mathf.Max(0, Mathf.FloorToInt(top / RowHeight));
            var lastRow = Mathf.Min(m_Timeline.rows.Count - 1, Mathf.FloorToInt(bottom / RowHeight));
            for (var i = firstRow; i <= lastRow; i++)
            {
                var row = m_Timeline.rows[i];
                var color = row.track == null ? GroupColor : i % 2 == 0 ? RowColor : AlternateRowColor;
                if (row.track != null && ReferenceEquals(row.track, m_SelectedTrack)) color = Color.Lerp(color, SelectionColor, 0.55f);
                FillRect(painter, new Rect(0f, i * RowHeight, width, RowHeight), color);
                Line(painter, new Vector2(0f, (i + 1) * RowHeight - 0.5f), new Vector2(width, (i + 1) * RowHeight - 0.5f), BorderColor);
            }
            // 网格覆盖整个可视区域，而不是只覆盖事件行或某个固定高度。
            var major = GetMajorTickInterval();
            var minor = major / 5d;
            var first = Math.Ceiling((m_ViewStart - TimeOffset) / minor);
            var count = Math.Min(2048, (int)Math.Ceiling(m_ViewDuration / minor) + 1);
            for (var i = 0; i < count; i++)
            {
                var tick = first + i;
                var x = TimeToPixel(tick * minor + TimeOffset);
                var color = GridColor;
                if (Math.Abs(tick % 5d) > 0.01d) color.a *= 0.4f;
                Line(painter, new Vector2(x, top), new Vector2(x, bottom), color);
            }
            for (var i = firstRow; i <= lastRow; i++) PaintRow(painter, i);
            var head = TimeToPixel(m_Playhead);
            if (head >= 0f && head <= width) Line(painter, new Vector2(head, top), new Vector2(head, bottom), PlayheadColor);
        }

        private void PaintRow(Painter2D painter, int rowIndex)
        {
            var row = m_Timeline.rows[rowIndex];
            var center = (rowIndex + 0.5f) * RowHeight;
            if (row.track == null)
            {
                foreach (var track in row.group.tracks)
                    PaintEvents(painter, track, center, true);
                return;
            }
            if (m_ShowSpans)
            {
                foreach (var span in row.track.spans)
                {
                    if (!row.track.visibleEvents.Contains(span.start)) continue;
                    var start = m_Timeline.events[span.start];
                    var end = span.end >= 0 ? m_Timeline.events[span.end].time : m_DataEnd;
                    var x1 = Mathf.Max(0f, TimeToPixel(start.time));
                    var x2 = Mathf.Min(CanvasWidth, TimeToPixel(end));
                    if (x2 <= x1) continue;
                    var color = GetEventColor(CapabilityDebugEventKind.Activated);
                    FillRect(painter, new Rect(x1, center - 6f, x2 - x1, 12f), Color.Lerp(RowColor, color, 0.38f));
                    Line(painter, new Vector2(x1, center - 6f), new Vector2(x2, center - 6f), color);
                    Line(painter, new Vector2(x1, center + 6f), new Vector2(x2, center + 6f), color);
                    if (span.end < 0)
                    {
                        // 未记录终点的区间使用斜线收尾，避免表现成完整持续时间。
                        for (var x = Mathf.Max(x1, x2 - 16f); x < x2; x += 5f)
                            Line(painter, new Vector2(x, center + 5f), new Vector2(Mathf.Min(x + 5f, x2), center - 5f), color);
                    }
                }
            }
            PaintEvents(painter, row.track, center, false);
        }

        private void PaintEvents(Painter2D painter, ACCTemporalTimeline.Track track, float y, bool summary)
        {
            var selected = -1;
            foreach (var index in track.visibleEvents)
            {
                var item = m_Timeline.events[index];
                if (item.sequence == m_SelectedSequence) { selected = index; continue; }
                var x = TimeToPixel(item.time);
                if (x < -7f || x > CanvasWidth + 7f) continue;
                Diamond(painter, x, y, summary ? 3.5f : 5f, GetEventColor(item.kind), false);
            }
            if (selected < 0) return;
            var selectedEvent = m_Timeline.events[selected];
            var selectedX = TimeToPixel(selectedEvent.time);
            if (selectedX >= -7f && selectedX <= CanvasWidth + 7f)
                Diamond(painter, selectedX, y, summary ? 4.5f : 6f, GetEventColor(selectedEvent.kind), true);
        }

        private void Diamond(Painter2D painter, float x, float y, float radius, Color color, bool selected)
        {
            painter.fillColor = selected ? Color.Lerp(color, Color.white, 0.4f) : color;
            painter.strokeColor = selected ? Color.white : BorderColor;
            painter.lineWidth = selected ? 1.5f : 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, y - radius));
            painter.LineTo(new Vector2(x + radius, y));
            painter.LineTo(new Vector2(x, y + radius));
            painter.LineTo(new Vector2(x - radius, y));
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();
        }

        private static void FillRect(Painter2D painter, Rect rect, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        private static void Line(Painter2D painter, Vector2 from, Vector2 to, Color color)
        {
            painter.strokeColor = color;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        private void RegisterTimelineInput(VisualElement target)
        {
            target.RegisterCallback<PointerDownEvent>(OnTimelinePointerDown);
            target.RegisterCallback<WheelEvent>(OnTimelineWheel, TrickleDown.TrickleDown);
            RegisterDragCallbacks(target);
        }

        private void RegisterDragCallbacks(VisualElement target)
        {
            target.RegisterCallback<PointerMoveEvent>(OnTimelinePointerMove);
            target.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != m_CapturedPointer) return;
                StopDrag();
                UpdateView();
                evt.StopPropagation();
            });
            target.RegisterCallback<PointerCaptureOutEvent>(evt =>
            {
                if (evt.pointerId == m_CapturedPointer) StopDrag();
            });
        }

        private void OnTimelinePointerDown(PointerDownEvent evt)
        {
            var target = (VisualElement)evt.currentTarget;
            target.Focus();
            if (evt.button == 2 || (evt.button == 0 && evt.altKey))
                BeginDrag(target, evt, DragMode.Pan);
            else if (evt.button == 1 && evt.altKey)
                BeginDrag(target, evt, DragMode.Zoom);
            else if (evt.button == 0)
            {
                SetPlaying(false);
                var point = target.WorldToLocal(evt.position);
                var row = Mathf.FloorToInt(point.y / RowHeight);
                var hit = target == m_Canvas ? m_Timeline.HitTest(row, PixelToTime(point.x),
                    7d / CanvasWidth * m_ViewDuration, m_SelectedSequence) : -1;
                if (hit >= 0)
                {
                    SelectEvent(hit);
                }
                else
                {
                    m_SelectedSequence = -1;
                    if (target == m_Canvas)
                    {
                        m_SelectedTrack = row >= 0 && row < m_Timeline.rows.Count ? m_Timeline.rows[row].track : null;
                        m_SelectedActor = row >= 0 && row < m_Timeline.rows.Count ? m_Timeline.rows[row].group.actor : Actor.none;
                    }
                    UpdateSelection();
                    UpdateComponents();
                    BeginDrag(target, evt, DragMode.Scrub);
                    Scrub(target, evt.position, evt.shiftKey);
                }
            }
            else return;
            evt.StopPropagation();
        }

        private void BeginDrag(VisualElement target, PointerDownEvent evt, DragMode mode)
        {
            StopDrag();
            if (mode != DragMode.Resize) m_FollowLatest = false;
            m_DragTarget = target;
            m_CapturedPointer = evt.pointerId;
            m_DragMode = mode;
            m_DragPosition = evt.position;
            m_DragViewStart = m_ViewStart;
            m_DragDuration = m_ViewDuration;
            m_DragScroll = m_TrackScroll.scrollOffset.y;
            m_DragTrackWidth = m_TrackWidth;
            target.EnableInClassList("acc-pan-cursor", mode == DragMode.Pan);
            target.EnableInClassList("acc-zoom-cursor", mode == DragMode.Zoom);
            target.EnableInClassList("acc-scrub-cursor", mode == DragMode.Scrub);
            target.CapturePointer(evt.pointerId);
        }

        private void StopDrag()
        {
            var target = m_DragTarget;
            var pointer = m_CapturedPointer;
            m_DragTarget = null;
            m_CapturedPointer = -1;
            m_DragMode = DragMode.None;
            target?.RemoveFromClassList("acc-pan-cursor");
            target?.RemoveFromClassList("acc-zoom-cursor");
            target?.RemoveFromClassList("acc-scrub-cursor");
            if (target != null && pointer >= 0 && target.HasPointerCapture(pointer)) target.ReleasePointer(pointer);
        }

        private void OnTimelinePointerMove(PointerMoveEvent evt)
        {
            if (m_DragMode == DragMode.None)
            {
                if (evt.currentTarget != m_Canvas) return;
                var point = m_Canvas.WorldToLocal(evt.position);
                var row = Mathf.FloorToInt(point.y / RowHeight);
                var hit = m_Timeline.HitTest(row, PixelToTime(point.x), 7d / CanvasWidth * m_ViewDuration);
                m_Canvas.tooltip = hit >= 0 ? FormatEvent(m_Timeline.events[hit]) : GetSpanTooltip(row, PixelToTime(point.x));
                return;
            }
            if (evt.pointerId != m_CapturedPointer) return;
            var delta = (Vector2)evt.position - m_DragPosition;
            switch (m_DragMode)
            {
                case DragMode.Resize:
                    SetTrackWidth(m_DragTrackWidth + delta.x);
                    break;
                case DragMode.Pan:
                    m_ViewStart = ClampViewStart(m_DragViewStart - delta.x / CanvasWidth * m_DragDuration);
                    m_TrackScroll.scrollOffset = new Vector2(0f, Mathf.Max(0f, m_DragScroll - delta.y));
                    UpdateView();
                    break;
                case DragMode.Zoom:
                    var x = m_DragTarget.WorldToLocal(m_DragPosition).x;
                    var anchor = m_DragViewStart + x / CanvasWidth * m_DragDuration;
                    m_ViewDuration = ClampDuration(m_DragDuration * Math.Exp(delta.x * 0.01d));
                    m_ViewStart = ClampViewStart(anchor - x / CanvasWidth * m_ViewDuration);
                    UpdateView();
                    break;
                case DragMode.Scrub:
                    Scrub(m_DragTarget, evt.position, evt.shiftKey);
                    break;
            }
            evt.StopPropagation();
        }

        private string GetSpanTooltip(int row, double time)
        {
            if (!m_ShowSpans || row < 0 || row >= m_Timeline.rows.Count) return string.Empty;
            var track = m_Timeline.rows[row].track;
            if (track == null) return string.Empty;
            foreach (var span in track.spans)
            {
                if (!track.visibleEvents.Contains(span.start)) continue;
                var start = m_Timeline.events[span.start].time;
                var end = span.end < 0 ? m_DataEnd : m_Timeline.events[span.end].time;
                if (time < start || time > end) continue;
                return $"{track.Name}\n激活：{start - TimeOffset:0.000}s\n" +
                    (span.end < 0 ? "未记录到结束事件；斜线表示开放区间，不保证当前仍激活。" :
                        $"结束：{end - TimeOffset:0.000}s  持续：{end - start:0.000}s");
            }
            return string.Empty;
        }

        private void Scrub(VisualElement target, Vector2 position, bool snap)
        {
            var time = PixelToTime(target.WorldToLocal(position).x);
            if (snap)
            {
                var distance = double.MaxValue;
                foreach (var index in m_Timeline.visibleEvents)
                {
                    var candidate = m_Timeline.events[index].time;
                    if (Math.Abs(candidate - time) >= distance) continue;
                    distance = Math.Abs(candidate - time);
                    m_Playhead = candidate;
                }
                if (distance < double.MaxValue) time = m_Playhead;
            }
            SetPlayhead(time, false);
        }

        private void OnTimelineWheel(WheelEvent evt)
        {
            var target = (VisualElement)evt.currentTarget;
            if (evt.ctrlKey || evt.commandKey)
            {
                m_TrackScroll.scrollOffset += new Vector2(0f, evt.delta.y * RowHeight);
            }
            else if (evt.shiftKey || Mathf.Abs(evt.delta.x) > Mathf.Abs(evt.delta.y))
            {
                m_FollowLatest = false;
                var delta = Mathf.Abs(evt.delta.x) > Mathf.Abs(evt.delta.y) ? evt.delta.x : evt.delta.y;
                m_ViewStart = ClampViewStart(m_ViewStart + delta * m_ViewDuration * 0.025d);
                UpdateView();
            }
            else ZoomAt(Math.Exp(evt.delta.y * 0.05d), target.WorldToLocal(evt.mousePosition).x);
            evt.StopImmediatePropagation();
        }

        private void ZoomAt(double factor, float pixel)
        {
            m_FollowLatest = false;
            var anchor = PixelToTime(pixel);
            m_ViewDuration = ClampDuration(m_ViewDuration * factor);
            m_ViewStart = ClampViewStart(anchor - pixel / CanvasWidth * m_ViewDuration);
            UpdateView();
        }

        private double ClampViewStart(double value)
            => Math.Max(m_DataStart, Math.Min(m_DataEnd, value));

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.altKey) return;
            var point = m_Canvas.WorldToLocal(evt.mousePosition);
            var hit = m_Timeline.HitTest(Mathf.FloorToInt(point.y / RowHeight), PixelToTime(point.x),
                7d / CanvasWidth * m_ViewDuration);
            if (hit >= 0) SelectEvent(hit);
            evt.menu.AppendAction("复制事件详情", _ => CopySelectedEvent(),
                m_Timeline.FindSequence(m_SelectedSequence) >= 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("复制可见事件（TSV）", _ => CopyVisibleEvents());
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!m_TimelineView.enabledSelf) return;
            // 输入框及其内部 TextElement 保留自身编辑快捷键。
            for (var element = evt.target as VisualElement; element != null; element = element.parent)
                if (element == m_ComponentView || element is TextField || element is DoubleField || element is ToolbarSearchField ||
                    element is DropdownField || element is Slider) return;
            if (evt.ctrlKey || evt.commandKey)
            {
                if (evt.keyCode != KeyCode.C) return;
                CopySelectedEvent();
            }
            else
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Space: SetPlaying(!IsPlaybackActive); break;
                    case KeyCode.LeftArrow: NavigateEvent(-1); break;
                    case KeyCode.RightArrow: NavigateEvent(1); break;
                    case KeyCode.Home: NavigateEvent(-2); break;
                    case KeyCode.End: NavigateEvent(2); break;
                    case KeyCode.Equals:
                    case KeyCode.KeypadPlus: ZoomAt(1d / 1.5d, CanvasWidth * 0.5f); break;
                    case KeyCode.Minus:
                    case KeyCode.KeypadMinus: ZoomAt(1.5d, CanvasWidth * 0.5f); break;
                    case KeyCode.Escape:
                        StopDrag();
                        m_SelectedSequence = -1;
                        m_SelectedTrack = null;
                        m_SelectedActor = Actor.none;
                        UpdateSelection();
                        UpdateComponents();
                        ResumeFollowing();
                        break;
                    default: return;
                }
            }
            evt.StopPropagation();
        }

        private static string GetEventName(CapabilityDebugEventKind kind)
        {
            switch (kind)
            {
                case CapabilityDebugEventKind.Added: return "添加";
                case CapabilityDebugEventKind.Activated: return "激活";
                case CapabilityDebugEventKind.Blocked: return "阻塞";
                case CapabilityDebugEventKind.Deactivated: return "停用";
                case CapabilityDebugEventKind.Removed: return "移除";
                case CapabilityDebugEventKind.Failed: return "异常";
                default: return kind.ToString();
            }
        }

        private Color GetEventColor(CapabilityDebugEventKind kind)
        {
            switch (kind)
            {
                case CapabilityDebugEventKind.Activated: return new Color(0.30f, 0.68f, 0.43f);
                case CapabilityDebugEventKind.Blocked: return new Color(0.86f, 0.60f, 0.20f);
                case CapabilityDebugEventKind.Failed: return new Color(0.90f, 0.30f, 0.27f);
                case CapabilityDebugEventKind.Deactivated: return new Color(0.32f, 0.58f, 0.86f);
                case CapabilityDebugEventKind.Removed: return new Color(0.64f, 0.46f, 0.74f);
                default: return MutedColor;
            }
        }
    }
}

#endif
