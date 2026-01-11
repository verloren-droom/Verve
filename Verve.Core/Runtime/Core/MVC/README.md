# `MVC` 框架使用指南

## 概述

`MVC（Model-View-Controller）`架构，旨在通过分层设计实现代码解耦，以提升代码的可维护性和复用性，适用于用户界面（GUI / HUD）场景。

## 核心概念

| 组件             | 职责                     |
|----------------|------------------------|
| **Model**      | 负责数据管理和事件通知机制          |
| **View**       | 处理用户界面显示、用户交互响应和生命周期管理 |
| **Controller** | 处理用户输入、协调业务逻辑和控制流程     |
| **Command**    | 实现可撤销操作和命令模式           |
| **Activity**   | 管理生命周期和组件协调            |

## 快速开始

### 1. 创建 `Activity`

```csharp
public class GameActivity : ActivityBase
{
    protected override void OnInitialized()
    {
        AddModel<PlayerModel>();
    
        AddCommand<MoveCommand>();
        AddCommand<LevelUpCommand>();
    }
}
```

### 2. 创建 `Model`

```csharp
using System;
using UnityEngine;

[Serializable]
public class PlayerModel : ModelBase
{
    [SerializeField] private int m_Health;
    [SerializeField] private int m_Level;

    public int Health => m_Health;
    public int Level => m_Level;

    public event Action<int> OnHealthChanged;
    public event Action<int> OnLevelUp;
    public event Action OnPlayerDied;

    public void SetHealth(int health)
    {
        m_Health = health;
        OnHealthChanged?.Invoke(m_Health);
    
        if (m_Health <= 0)
            OnPlayerDied?.Invoke();
    }

    public void AddExperience(int exp)
    {
        int newLevel = Mathf.FloorToInt(Mathf.Sqrt(exp / 100f)) + 1;
        if (newLevel > m_Level)
        {
            m_Level = newLevel;
            OnLevelUp?.Invoke(m_Level);
        }
    }

    protected override void OnDisposed()
    {
        OnHealthChanged = null;
        OnLevelUp = null;
        OnPlayerDied = null;
    }
}
```

### 3. 创建 `View`

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerInfoView : ViewBase
{
    [SerializeField] private Text m_HealthText;
    [SerializeField] private Button m_LevelUpButton;
    
    private PlayerModel m_PlayerModel;
    
    public override IActivity GetActivity() => GameActivity.Instance;
    
    protected override void OnOpening(params object[] args)
    {
        m_PlayerModel = GetActivity().GetModel<PlayerModel>();
        m_PlayerModel.OnHealthChanged += OnHealthChanged;
        m_PlayerModel.OnLevelUp += OnLevelUp;
        
        UpdateUI();
        AddEventTrigger(m_LevelUpButton, EventTriggerType.PointerClick, OnLevelUpClicked);
    }
    
    protected override void OnClosing()
    {
        if (m_PlayerModel != null)
        {
            m_PlayerModel.OnHealthChanged -= OnHealthChanged;
            m_PlayerModel.OnLevelUp -= OnLevelUp;
        }
        RemoveAllTriggers(m_LevelUpButton);
    }
    
    private void OnHealthChanged(int health) => UpdateUI();
    private void OnLevelUp(int level) => UpdateUI();
    
    private void OnLevelUpClicked(BaseEventData e)
    {
        GetActivity().ExecuteCommand<LevelUpCommand>();
    }
    
    private void UpdateUI()
    {
        if (m_PlayerModel == null) return;
        m_HealthText.text = $"Health: {m_PlayerModel.Health}";
    }
}
```

### 4. 创建 `Controller`

```csharp
using System;
using UnityEngine;

public class PlayerController : MonoBehaviour, IController
{
    private PlayerModel m_PlayerModel;

    public IActivity GetActivity() => GameActivity.Instance;

    public void Initialize()
    {
        m_PlayerModel = this.GetModel<PlayerModel>();
    }

    public void MoveTo(Vector3 position)
    {
        GetActivity().ExecuteCommand<MoveCommand>();
    }

    public void TakeDamage(int damage)
    {
        if (m_PlayerModel == null) return;
        m_PlayerModel.SetHealth(m_PlayerModel.Health - damage);
    }
}
```

### 5. 创建 `Command`

```csharp
using UnityEngine;

public class MoveCommand : CommandBase
{
    private Vector3 m_OldPosition;
    private Vector3 m_NewPosition;

    public override IActivity GetActivity() => GameActivity.Instance;

    protected override void OnExecute()
    {
        var player = GameObject.FindObjectOfType<PlayerController>();
        if (player == null) return;
    
        m_OldPosition = player.transform.position;
        m_NewPosition = m_OldPosition + Vector3.forward;
        player.transform.position = m_NewPosition;
    }

    protected override void OnUndo()
    {
        var player = GameObject.FindObjectOfType<PlayerController>();
        if (player == null) return;
        player.transform.position = m_OldPosition;
    }
}

public class LevelUpCommand : CommandBase
{
    private int m_OldLevel;

    public override IActivity GetActivity() => GameActivity.Instance;

    protected override void OnExecute()
    {
        var playerModel = this.GetModel<PlayerModel>();
        m_OldLevel = playerModel.Level;
        playerModel.AddExperience(100);
    }
}
```

### 6. 使用 `Activity` 管理视图

```csharp
using System;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    [SerializeField] private GameObject m_MainMenuPrefab;
    [SerializeField] private GameObject m_PlayerInfoPrefab;
    
    private void Start()
    {
        GameActivity.Instance.OpenView<MainMenuView>(m_MainMenuPrefab);
    }
    
    public void ShowPlayerInfo()
    {
        GameActivity.Instance.OpenView<PlayerInfoView>(m_PlayerInfoPrefab);
    }
    
    public void GoBack()
    {
        GameActivity.Instance.GoBackView();
    }
    
    public void CloseAllViews()
    {
        GameActivity.Instance.CloseViewAll();
    }
}
```