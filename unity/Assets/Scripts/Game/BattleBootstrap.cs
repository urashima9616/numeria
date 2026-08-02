using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 启动引导:进入任意场景后自动搭建战斗(纯程序化,不依赖场景内容)。
    /// </summary>
    public static class BattleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<MapController>() != null) return;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            new GameObject("ForestMap").AddComponent<MapController>();
        }
    }
}
