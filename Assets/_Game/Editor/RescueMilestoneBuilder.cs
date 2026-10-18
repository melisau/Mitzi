#if UNITY_EDITOR
using UnityEditor;
using PawPath.Data;
[InitializeOnLoad]
public static class RescueMilestoneBuilder
{
    static RescueMilestoneBuilder()=>EditorApplication.delayCall+=Apply;
    public static void Apply()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");if(catalog==null)return;
        string[] ids={"pamuk","komur","tarcin","ada","moka"};
        int[] milestones={7,20,30,40,50};
        for(int i=0;i<ids.Length;i++)
        {
            var cat=catalog.GetCat(ids[i]);if(cat==null)continue;
            if(cat.unlockAfterLevel==milestones[i])continue;
            cat.unlockAfterLevel=milestones[i];EditorUtility.SetDirty(cat);
        }
        EditorSave.Save(catalog);
    }
}
#endif
