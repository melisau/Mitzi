#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Levels;
using PawPath.Gameplay;
using PawPath.Cat;
using PawPath.UI;

public static class DifficultyProgressionValidation
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated project");
            HomeSettingsValidation.Run();RescueMilestoneBuilder.Apply();SaveServiceValidation.Run();
            var flow=GameFlow.Instance;var data=SaveService.Data;var catalog=flow.Catalog;
            int[] milestones={7,20,30,40,50};string[] ids={"pamuk","komur","tarcin","ada","moka"};
            for(int i=0;i<ids.Length;i++)
            {
                Check(catalog.GetCat(ids[i]).unlockAfterLevel==milestones[i],"Wrong catalog rescue milestone");
                Check(CatUnlockService.WaitingAtLevel(milestones[i]-1)==null,"Rescue appears early");
                Check(CatUnlockService.WaitingAtLevel(milestones[i])?.id==ids[i],"Gate cat missing");
                Check(CatUnlockService.TryUnlockAfterLevel(milestones[i],out var rescued)&&rescued.id==ids[i],"Rescue failed");
                Check(!CatUnlockService.TryUnlockAfterLevel(milestones[i],out _),"Duplicate rescue");
            }
            data.unlockedCatIds.Remove("pamuk");
            Check(CatUnlockService.WaitingAtLevel(9)?.id=="pamuk"&&CatUnlockService.TryUnlockAfterLevel(9,out _),"Old save missed moved milestone");
            Check(SaveService.TryReadJson("{\"schemaVersion\":4,\"lovePoints\":777,\"highestCompletedLevel\":99,\"unlockedCatIds\":[\"mitzi\",\"pamuk\"]}",out var old,out _)&&old.lovePoints==777&&old.highestCompletedLevel==99&&old.unlockedCatIds.Contains("pamuk")&&old.themeCompletedLevels.Length==4&&old.themeCompletedLevels.All(n=>n==0),"Legacy migration lost data or invented theme history");
            GameDifficulty.SelectAutomatic();
            data.themeCompletedLevels=new int[4];
            foreach(int completed in new[]{0,9,10,19,20,55})
            {
                data.themeCompletedLevels[0]=completed;
                var expected=completed<10?DifficultyLevel.Easy:completed<20?DifficultyLevel.Medium:DifficultyLevel.Hard;
                Check(GameDifficulty.ForTheme(0)==expected&&GameDifficulty.ForTheme(1)==DifficultyLevel.Easy,"Automatic boundary or per-theme isolation failed");
            }
            foreach(var difficulty in new[]{DifficultyLevel.Easy,DifficultyLevel.Medium,DifficultyLevel.Hard})
            {
                GameDifficulty.Select(difficulty);
                Check(!GameDifficulty.IsAutomatic&&Enumerable.Range(0,4).All(theme=>GameDifficulty.ForTheme(theme)==difficulty),"Manual override failed");
            }
            SaveService.Persist();
            Check(SaveService.TryReadJson(SaveService.ExportJson(),out var roundTrip,out _)&&roundTrip.themeCompletedLevels.SequenceEqual(data.themeCompletedLevels),"Theme progress did not persist");
            var buttons=flow.GetComponentsInChildren<Button>(true);
            buttons.First(b=>b.name=="EasyDifficulty").onClick.Invoke();Check(!GameDifficulty.IsAutomatic&&GameDifficulty.Selected==DifficultyLevel.Easy,"Manual UI selection failed");
            buttons.First(b=>b.name=="AutomaticDifficulty").onClick.Invoke();Check(GameDifficulty.IsAutomatic,"Automatic UI selection failed");
            var level=flow.GetComponent<LevelManager>();Call(level,"Awake");
            var cat=flow.GetComponentInChildren<CatController>(true);Call(cat,"Awake");
            data.highestCompletedLevel=5;data.themeCompletedLevels=new[]{0,9,0,0};
            PlayerPrefs.SetInt("PawPath.SelectedTheme",1);GameplayMode.Select(GameplayPlayMode.DirectControl);
            level.BeginCurrentLevel();level.BeginCurrentLevel();
            Check(data.themeCompletedLevels[1]==9&&level.CurrentDifficulty==DifficultyLevel.Easy,"Retry increased theme progress");
            PlayerPrefs.SetInt("PawPath.SelectedTheme",2);level.CompleteLevel();level.CompleteLevel();
            Check(data.highestCompletedLevel==6&&data.themeCompletedLevels[1]==10&&data.themeCompletedLevels[2]==0,"Completion used wrong theme or counted twice");
            Check(GameDifficulty.ForTheme(1)==DifficultyLevel.Medium&&GameDifficulty.ForTheme(2)==DifficultyLevel.Easy,"Completing tenth theme level did not advance difficulty");
            var course=flow.GetComponentInChildren<LevelCourseBuilder>(true);
            PlayerPrefs.SetInt("PawPath.SelectedTheme",0);Call(level,"ApplyLevelTheme",170);
            int lastGaps=0;float lastSpeed=0;
            foreach(var difficulty in new[]{DifficultyLevel.Easy,DifficultyLevel.Medium,DifficultyLevel.Hard})
            {
                GameDifficulty.Select(difficulty);course.Build(170);
                int gaps=((ICollection)typeof(LevelCourseBuilder).GetField("gaps",Private).GetValue(course)).Count;
                float speed=course.GetComponentsInChildren<DogObstacle>().Max(d=>(float)typeof(DogObstacle).GetField("speed",Private).GetValue(d));
                Check(gaps>lastGaps&&speed>lastSpeed&&course.CurrentDifficulty==difficulty,"Difficulty does not affect course and enemies");
                var road=course.GetComponentsInChildren<BoxCollider2D>().Where(c=>c.name=="Road").Select(c=>c.transform.position.x).ToArray();
                course.Build(170);
                Check(road.SequenceEqual(course.GetComponentsInChildren<BoxCollider2D>().Where(c=>c.name=="Road").Select(c=>c.transform.position.x)),"Retry changed course layout");
                lastGaps=gaps;lastSpeed=speed;
            }
            GameDifficulty.SelectAutomatic();flow.EnterHub();
            var automatic=buttons.First(b=>b.name=="AutomaticDifficulty");automatic.onClick.Invoke();automatic.transform.parent.gameObject.SetActive(true);
            foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true))canvas.sortingOrder=200;
            Canvas.ForceUpdateCanvases();var camera=Camera.main;camera.aspect=16f/9f;
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/difficulty-menu-preview.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("DIFFICULTY_PROGRESSION_AND_RESCUE_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
