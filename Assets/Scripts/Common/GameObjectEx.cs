using UnityEngine;

public static class GameObjectEx
{
    public static GameObject FindChild(this GameObject pRoot, string pName)
    {
        var childTransform = pRoot.transform.Find(pName);
        return childTransform == null ? null : childTransform.gameObject;
    }
}
