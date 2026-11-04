#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DesktopDoomsday.EditorTools
{
    /// <summary>
    /// 一键清理当前场景中所有「Missing Script」组件（场景里残留的失效脚本引用）。
    /// 典型症状：控制台报 "The referenced script (Unknown) on this Behaviour is missing!"，
    /// 但编译无错误——说明是场景文件里某个 GameObject 挂了一个 GUID 找不到的组件。
    ///
    /// 原理：Missing script 在 Unity 内部表现为 GetComponents&lt;Component&gt;() 返回的数组里出现 null 元素
    /// （组件对象存在但脚本类找不到）。本工具遍历场景所有 GameObject（含 inactive），找到 null 组件并移除。
    ///
    /// 菜单：DesktopDoomsday → 清理 Missing Script 组件。
    /// 执行后会标记场景为已修改（需 Ctrl+S 保存），并报告清除了多少个。
    /// </summary>
    public static class CleanupMissingScripts
    {
        [MenuItem("DesktopDoomsday/清理 Missing Script 组件")]
        private static void Cleanup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                Debug.LogWarning("[CleanupMissingScripts] 当前没有加载的场景。");
                return;
            }

            int totalRemoved = 0;
            int affectedObjects = 0;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                RemoveMissingFromTree(root, ref totalRemoved, ref affectedObjects);
            }

            if (totalRemoved > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[CleanupMissingScripts] 已从 {affectedObjects} 个 GameObject 上移除 {totalRemoved} 个 Missing Script 组件。请 Ctrl+S 保存场景。");
                EditorUtility.DisplayDialog("清理完成",
                    $"已从 {affectedObjects} 个 GameObject 上移除 {totalRemoved} 个 Missing Script 组件。\n请按 Ctrl+S 保存场景。",
                    "确定");
            }
            else
            {
                Debug.Log("[CleanupMissingScripts] 当前场景未发现 Missing Script 组件。");
                EditorUtility.DisplayDialog("清理完成", "当前场景未发现 Missing Script 组件。", "确定");
            }
        }

        /// <summary>递归遍历 GameObject 树，移除每个节点上的 missing script 组件。</summary>
        private static void RemoveMissingFromTree(GameObject go, ref int totalRemoved, ref int affectedObjects)
        {
            if (go == null) return;

            // 检查本节点：GetComponents<Component>() 数组里的 null 元素就是 missing script
            int removedHere = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removedHere > 0)
            {
                totalRemoved += removedHere;
                affectedObjects++;
                Debug.Log($"[CleanupMissingScripts] {GetPath(go)}：移除 {removedHere} 个 missing 组件", go);
            }

            // 递归子节点
            for (int i = 0; i < go.transform.childCount; i++)
            {
                RemoveMissingFromTree(go.transform.GetChild(i).gameObject, ref totalRemoved, ref affectedObjects);
            }
        }

        /// <summary>获取 GameObject 在场景中的完整路径（便于定位）。</summary>
        private static string GetPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
#endif
