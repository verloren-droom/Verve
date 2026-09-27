#if UNITY_5_3_OR_NEWER

namespace Verve.Tests.ACC
{
    using System;
    using UnityEngine;
    using NUnit.Framework;
    using Object = UnityEngine.Object;

    /// <summary>
    ///   <para>行动者对象测试；验证 Unity 生命周期与世界所有权边界。</para>
    /// </summary>
    [Category("ACC")]
    internal sealed class ActorObjectTests
    {
        /// <summary>
        ///   <para>对象销毁时释放其行动者。</para>
        /// </summary>
        [Test]
        public void DestroyingActorObject_DestroysActor()
        {
            var world = new World("ActorObject.Destroy");
            var gameObject = new GameObject("ActorObject");
            try
            {
                var actorObject = gameObject.AddComponent<ActorObject>();
                actorObject.Create(world);
                var actor = actorObject.Actor;

                Assert.That(world.IsActorAlive(actor), Is.True);
                Object.DestroyImmediate(gameObject);
                Assert.That(world.IsActorAlive(actor), Is.False);
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                world.Dispose();
            }
        }

        /// <summary>
        ///   <para>禁用对象时保留行动者，兼容对象池回收。</para>
        /// </summary>
        [Test]
        public void DisablingActorObject_PreservesActor()
        {
            var world = new World("ActorObject.Disable");
            var gameObject = new GameObject("ActorObject");
            try
            {
                var actorObject = gameObject.AddComponent<ActorObject>();
                actorObject.Create(world);
                var actor = actorObject.Actor;

                gameObject.SetActive(false);
                Assert.That(world.IsActorAlive(actor), Is.True);
                gameObject.SetActive(true);
                Assert.That(actorObject.Actor, Is.EqualTo(actor));
                Assert.That(world.Actors.AliveActorCount, Is.EqualTo(1));
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                world.Dispose();
            }
        }

        /// <summary>
        ///   <para>世界清空时清除行动者对象的运行时引用。</para>
        /// </summary>
        [Test]
        public void ClearingWorld_DetachesActorObject()
        {
            var world = new World("ActorObject.Clear");
            var gameObject = new GameObject("ActorObject");
            try
            {
                var actorObject = gameObject.AddComponent<ActorObject>();
                actorObject.Create(world);

                world.Clear();

                Assert.That(actorObject.IsCreated, Is.False);
                Assert.That(actorObject.World, Is.Null);
                Assert.That(actorObject.Actor.IsNone, Is.True);
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                world.Dispose();
            }
        }

        /// <summary>
        ///   <para>重复指定同一世界不会重复创建行动者。</para>
        /// </summary>
        [Test]
        public void CreatingAgainInSameWorld_IsIdempotent()
        {
            var world = new World("ActorObject.Idempotent");
            var gameObject = new GameObject("ActorObject");
            try
            {
                var actorObject = gameObject.AddComponent<ActorObject>();
                actorObject.Create(world);
                var actor = actorObject.Actor;

                actorObject.Create(world);

                Assert.That(actorObject.Actor, Is.EqualTo(actor));
                Assert.That(world.Actors.AliveActorCount, Is.EqualTo(1));
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                world.Dispose();
            }
        }

        /// <summary>
        ///   <para>切换到另一世界会拒绝操作并保留原有所有权。</para>
        /// </summary>
        [Test]
        public void CreatingInAnotherWorld_RejectsWithoutChangingOwnership()
        {
            var firstWorld = new World("ActorObject.First");
            var secondWorld = new World("ActorObject.Second");
            var gameObject = new GameObject("ActorObject");
            try
            {
                var actorObject = gameObject.AddComponent<ActorObject>();
                actorObject.Create(firstWorld);
                var actor = actorObject.Actor;

                Assert.Throws<InvalidOperationException>(() => actorObject.Create(secondWorld));

                Assert.That(actorObject.Actor, Is.EqualTo(actor));
                Assert.That(actorObject.World, Is.SameAs(firstWorld));
                Assert.That(firstWorld.IsActorAlive(actor), Is.True);
                Assert.That(secondWorld.Actors.AliveActorCount, Is.Zero);
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                firstWorld.Dispose();
                secondWorld.Dispose();
            }
        }

        /// <summary>
        ///   <para>创建失败后允许再次执行，避免残留创建状态。</para>
        /// </summary>
        [Test]
        public void CreatingInDisposedWorld_DoesNotLeaveCreatingState()
        {
            var world = new World("ActorObject.Disposed");
            var gameObject = new GameObject("ActorObject");
            try
            {
                world.Dispose();
                var actorObject = gameObject.AddComponent<ActorObject>();

                Assert.Throws<ObjectDisposedException>(() => actorObject.Create(world));
                Assert.Throws<ObjectDisposedException>(() => actorObject.Create(world));
                Assert.That(actorObject.IsCreated, Is.False);
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                world.Dispose();
            }
        }

        /// <summary>
        ///   <para>世界释放时解除行动者对象引用。</para>
        /// </summary>
        [Test]
        public void DisposingWorld_DetachesActorObject()
        {
            var world = new World("ActorObject.Dispose");
            var gameObject = new GameObject("ActorObject");
            try
            {
                var actorObject = gameObject.AddComponent<ActorObject>();
                actorObject.Create(world);

                world.Dispose();

                Assert.That(actorObject.IsCreated, Is.False);
                Assert.That(actorObject.World, Is.Null);
                Assert.That(actorObject.Actor.IsNone, Is.True);
            }
            finally
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
                world.Dispose();
            }
        }
    }
}

#endif
