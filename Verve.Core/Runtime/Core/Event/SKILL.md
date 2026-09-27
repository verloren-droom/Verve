---
name: verve-event-bus
description: Use Verve's global event bus for typed publish/subscribe communication with explicit listener ownership and disposal.
---

# Scope

The event bus is exposed through Game.On, Game.Emit, and Game.Off. Event keys may be strings or integers; payloads are generic arguments rather than untyped dictionaries.

## Subscribe

An ownerless subscription returns IDisposable and must be retained until the listener is no longer needed:

~~~csharp
using System;
using UnityEngine;
using Verve;

public sealed class PlayerHud : MonoBehaviour
{
    private IDisposable m_DamageSubscription;

    private void OnEnable()
    {
        m_DamageSubscription = Game.On<int>("PlayerDamaged", OnPlayerDamaged);
    }

    private void OnDisable()
    {
        m_DamageSubscription?.Dispose();
        m_DamageSubscription = null;
    }

    private void OnPlayerDamaged(int amount) { }
}
~~~

For a MonoBehaviour, the owner overload binds the listener to that component's lifetime:

~~~csharp
Game.On("PlayerDied", OnPlayerDied, this);
~~~

Use one ownership style for a listener. Do not call Game.Off(key) as a substitute for disposing one subscription: that overload removes every listener registered for the key.

## Publish

~~~csharp
Game.Emit("PlayerDied");
Game.Emit("PlayerDamaged", 10);
~~~

Define keys in one place (constants or a dedicated key type) so publishers and subscribers cannot drift through scattered string literals.

## Rules

- Match the Emit payload shape with the On callback generic signature. Conflicting signatures, duplicate registrations, and listener failures throw.
- String keys retain their full value and are independent of integer keys; do not pre-hash strings.
- Dispose ownerless subscriptions in the same lifecycle that created them.
- Use Game.Off(key, handler) only when removing a known handler explicitly.
- Do not use the bus for direct synchronous calls between objects that already have a concrete dependency.
- Emit invokes callbacks synchronously on the caller thread; emit events that touch Unity objects from the Unity main thread.
