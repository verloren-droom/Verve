// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>行动者对象登记表；由 <see cref="World"/> 独占拥有。</para>
    /// </summary>
    [Serializable]
    internal sealed class ActorObjectRegistry
    {
        /// <summary>
        ///   <para>对象登记项；回调由登记方提供，释放由世界触发。</para>
        /// </summary>
        private sealed class Registration
        {
            /// <summary>
            ///   <para>外部对象。</para>
            /// </summary>
            internal readonly IActorObject Object;
            /// <summary>
            ///   <para>解除对象关联的内部回调。</para>
            /// </summary>
            internal readonly Action Detach;

            /// <summary>
            ///   <para>创建对象登记项。</para>
            /// </summary>
            /// <param name="actorObject">外部对象。</param>
            /// <param name="detach">解除回调。</param>
            internal Registration(IActorObject actorObject, Action detach)
            {
                Object = actorObject ?? throw new ArgumentNullException(nameof(actorObject));
                Detach = detach ?? throw new ArgumentNullException(nameof(detach));
            }
        }

        /// <summary>
        ///   <para>按行动者保存外部对象。</para>
        /// </summary>
        private readonly Dictionary<Actor, List<Registration>> m_Objects = new(64);
        /// <summary>
        ///   <para>正在解除对象关联的行动者；防止回调中重新登记。</para>
        /// </summary>
        private readonly HashSet<Actor> m_DetachingActors = new();

        /// <summary>
        ///   <para>登记一个对象。</para>
        /// </summary>
        /// <param name="actor">对象对应的行动者。</param>
        /// <param name="actorObject">外部对象。</param>
        internal void Add(Actor actor, IActorObject actorObject, Action detach)
        {
            if (actorObject == null) throw new ArgumentNullException(nameof(actorObject));
            if (detach == null) throw new ArgumentNullException(nameof(detach));
            if (actorObject.Actor != actor)
                throw new InvalidOperationException("The actor object does not match the requested actor.");
            if (m_DetachingActors.Contains(actor))
                throw new InvalidOperationException("The actor objects are being detached.");

            if (!m_Objects.TryGetValue(actor, out var objects))
                m_Objects.Add(actor, objects = new List<Registration>(1));
            for (var i = 0; i < objects.Count; i++)
                if (ReferenceEquals(objects[i].Object, actorObject))
                    throw new InvalidOperationException("The actor object is already registered.");

            objects.Add(new Registration(actorObject, detach));
        }

        /// <summary>
        ///   <para>移除一个对象并通知其解除。</para>
        /// </summary>
        /// <param name="actorObject">外部对象。</param>
        /// <returns>是否找到并移除。</returns>
        internal bool Remove(IActorObject actorObject)
        {
            if (actorObject == null || !m_Objects.TryGetValue(actorObject.Actor, out var objects))
                return false;

            var index = -1;
            for (var i = 0; i < objects.Count; i++)
            {
                if (!ReferenceEquals(objects[i].Object, actorObject)) continue;
                index = i;
                break;
            }
            if (index < 0) return false;
            var registration = objects[index];
            objects.RemoveAt(index);
            if (objects.Count == 0) m_Objects.Remove(actorObject.Actor);
            registration.Detach();
            return true;
        }

        /// <summary>
        ///   <para>解除指定行动者的全部对象。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        internal void RemoveActor(Actor actor)
        {
            if (!m_Objects.TryGetValue(actor, out var objects)) return;
            m_Objects.Remove(actor);
            m_DetachingActors.Add(actor);
            List<Exception> errors = null;
            try
            {
                for (int i = objects.Count - 1; i >= 0; i--)
                {
                    try { objects[i].Detach(); }
                    catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                }
            }
            finally
            {
                m_DetachingActors.Remove(actor);
            }

            if (errors != null)
                throw new AggregateException("One or more actor objects could not be detached.", errors);
        }

        /// <summary>
        ///   <para>解除全部对象并清空登记表。</para>
        /// </summary>
        internal void Clear()
        {
            if (m_Objects.Count == 0) return;

            var actors = new Actor[m_Objects.Count];
            m_Objects.Keys.CopyTo(actors, 0);
            List<Exception> errors = null;
            for (int i = 0; i < actors.Length; i++)
            {
                try { RemoveActor(actors[i]); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }

            if (errors != null)
                throw new AggregateException("One or more actor objects could not be cleared.", errors);
        }
    }
}