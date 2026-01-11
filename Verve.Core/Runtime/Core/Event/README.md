# `EventBus` 事件总线

## 概述

事件总线系统，采用字符串和整数作为事件键，自动管理事件生命周期。

## 快速开始

### 1. 监听事件

```csharp
using Verve;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void Start()
    {
        // 监听无参数事件
        Game.On("PlayerDied", OnPlayerDied);

        // 监听带参数事件
        Game.On<int>("PlayerTakeDamage", OnPlayerTakeDamage);

        // 自动管理生命周期（随 MonoBehaviour 销毁）
        Game.On("PlayerJumped", OnPlayerJumped, this);
    }

    private void OnPlayerDied()
    {
        Debug.Log("Player died!");
    }

    private void OnPlayerTakeDamage(int damage)
    {
        Debug.Log($"Player took {damage} damage");
    }

    private void OnPlayerJumped()
    {
        Debug.Log("Player jumped!");
    }
}
```

### 2. 发送事件

```csharp
using Verve;
using UnityEngine;


public class EnemyController : MonoBehaviour
{
    public void AttackPlayer(int damage)
    {
        Game.Emit("PlayerTakeDamage", damage);
    }

    public void KillPlayer()
    {
        Game.Emit("PlayerDied");
    }
}
```

### 3. 取消监听

```csharp
using Verve;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void OnDestroy()
    {
        // 取消所有监听
        Game.Off("PlayerDied");
        Game.Off("PlayerTakeDamage");
        Game.Off("PlayerJumped");
    }
}
```