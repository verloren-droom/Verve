#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using System;
    using UnityEngine;
    using System.Collections;
    
    
    /// <summary>
    ///  Gameplay 游戏世界
    /// </summary>
    public partial class World : ComponentInstanceBase<MonoBehaviour>
    {
        [SerializeField, Tooltip("游戏模式")] private GameMode m_GameMode;
        [SerializeField, Tooltip("游戏状态")] private GameState m_GameState;
        
        public GameMode GameMode => m_GameMode;
        public GameState GameState => m_GameState;

        private IEnumerator Start()
        {
            yield return null;
            AddListenGameInited(StartGame);
            InitGame();
        }
        
        public void InitGame() => m_GameMode?.InitGame();
        public void StartGame() => m_GameMode?.StartGame();
        public void EndGame() => m_GameMode?.EndGame();

        public void AddListenGameInited(Action callback)
        {
            if (m_GameMode != null)
            {
                m_GameMode.OnGameInited += callback;
            }
        }

        private void OnDestroy()
        {
            m_GameMode?.EndGame();
            m_GameState?.ResetState();
        }
    }
}

#endif