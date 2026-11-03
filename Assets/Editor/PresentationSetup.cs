using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using CuatroPatasVR.Training;

public static class PresentationSetup
{
    const string Root = "Assets/PresentacionVR";
    [MenuItem("Tools/Cuatro Patas/Aplicar presentacion visual")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new Exception("Salir de Play antes de aplicar.");
        var previous = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");
        if(previous.isLoaded) EditorSceneManager.CloseScene(previous,true);
        AssetDatabase.DeleteAsset(Root + "/Animaciones");
        Directory.CreateDirectory(Root + "/Animaciones");
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/FondoHogar.png");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        EditMenu();
        EditTraining();
        AssetDatabase.SaveAssets();
        File.WriteAllText("/Users/yamiurbano/Documents/Codex/2026-11-02/re/work/presentation-done.txt", "Menu y EntrenamientoVR guardados; CasaVR sin cambios.");
        Debug.Log("PRESENTACION_VR_OK");
    }
    static AnimationClip Clip(string name)
    {
        var clip = new AnimationClip { name = name, frameRate = 30 };
        AssetDatabase.CreateAsset(clip, Root + "/Animaciones/" + name + ".anim");
        return clip;
    }
    static void Curve(AnimationClip clip, Type type, string property, params Keyframe[] keys)
    { AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", type, property), new AnimationCurve(keys)); }
    static void Loop(AnimationClip clip)
    { var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings); }
    static Animator Attach(GameObject go, AnimationClip clip)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animaciones/" + clip.name + ".controller");
        controller.AddMotion(clip);
        var animator = go.GetComponent<Animator>();
        if(animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        return animator;
    }
    static void EditMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Additive);
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var legacy = all.Select(t => t.GetComponent<MenuAnimator>()).First(x => x != null);
        var image = legacy.background.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/FondoHogar.png");
        image.color = Color.white;
        legacy.background.localScale = Vector3.one;
        // Keep the existing responsive layout, mascot and brand artwork.
        var subtitle = legacy.subtitle.GetComponent<TMP_Text>();
        subtitle.text = "Cuida a tu gato. Entrena tus reflejos.";
        subtitle.color = new Color(0.24f, 0.15f, 0.09f);
        var logo = Clip("LogoRespira");
        float y = legacy.logo.anchoredPosition.y;
        Curve(logo, typeof(RectTransform), "m_AnchoredPosition.y", new Keyframe(0,y), new Keyframe(2,y+5), new Keyframe(4,y));
        Loop(logo); Attach(legacy.logo.gameObject, logo);
        var cat = Clip("GatoRespira");
        Curve(cat, typeof(Transform), "m_LocalScale.y", new Keyframe(0,1),new Keyframe(1.5f,1.012f),new Keyframe(3,1));
        Loop(cat); Attach(legacy.cat.gameObject, cat);
        var canvas = legacy.GetComponent<CanvasGroup>();
        if(canvas == null) canvas = legacy.gameObject.AddComponent<CanvasGroup>();
        canvas.alpha = 1;
        var intro = Clip("EntradaMenu");
        Curve(intro,typeof(CanvasGroup),"m_Alpha",new Keyframe(0,0),new Keyframe(0.6f,1));
        Attach(legacy.gameObject,intro);
        foreach (var fx in all.Select(t=>t.GetComponent<MenuButtonFX>()).Where(x=>x!=null))
        {
            // Native Button colour transitions replace per-frame scale code.
            var button=fx.GetComponent<Button>();
            if(button!=null) {
                button.transition=Selectable.Transition.ColorTint;
                var colors=button.colors;
                colors.normalColor=Color.white; colors.highlightedColor=new Color(1f,.88f,.68f);
                colors.pressedColor=new Color(.78f,.65f,.48f); colors.selectedColor=colors.highlightedColor; colors.fadeDuration=.12f;
                button.colors=colors;
                if(fx.clickClip!=null) {
                    var audio=fx.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.clip=fx.clickClip;audio.volume=fx.volume;
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,audio.Play);
                }
            }
            UnityEngine.Object.DestroyImmediate(fx);
        }
        var loader=all.Select(t=>t.GetComponent<SceneLoader>()).First(x=>x!=null);
        foreach(var t in all) {
            if(t.name!="BotonCorrer" && t.name!="BotonJugar") continue;
            var b=t.GetComponent<Button>();if(b==null)continue;
            for(int i=b.onClick.GetPersistentEventCount()-1;i>=0;i--)
                if(b.onClick.GetPersistentTarget(i) is SceneLoader) UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick,i);
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(b.onClick,loader.LoadScene,t.name=="BotonCorrer"?"EntrenamientoVR":"casaVR");
            var label=t.GetComponentInChildren<TMP_Text>();if(label!=null)label.text=t.name=="BotonCorrer"?"Entrenamiento VR":"Cuidar a mi gato";
        }
        foreach(var text in all.Select(t=>t.GetComponent<TMP_Text>()).Where(x=>x!=null))
            if(text.text.Contains("<b>EN CASA</b>")) text.text="<b>CUIDA A TU GATO</b>\nVisita su hogar y atiende sus necesidades.\n\n<b>ENTRENAMIENTO VR</b>\nElige Práctica o Modo libre mirando una tarjeta y pulsando el gatillo. Golpea con la mano indicada, agáchate y esquiva.\n\nEn modo libre, mantén B o Y durante un segundo para terminar.";
        UnityEngine.Object.DestroyImmediate(legacy);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene,true);
    }
    static void EditTraining()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/EntrenamientoVR.unity",OpenSceneMode.Additive);
        var normal=Clip("TarjetaReposo");var focus=Clip("TarjetaEnfocada");
        var colors=new[]{new Color(.12f,.23f,.17f),new Color(.29f,.43f,.30f)};
        for(int i=0;i<2;i++) {
            var c=colors[i];var clip=i==0?normal:focus;
            Curve(clip,typeof(MeshRenderer),"material._BaseColor.r",new Keyframe(0,c.r));
            Curve(clip,typeof(MeshRenderer),"material._BaseColor.g",new Keyframe(0,c.g));
            Curve(clip,typeof(MeshRenderer),"material._BaseColor.b",new Keyframe(0,c.b));
            Curve(clip,typeof(MeshRenderer),"material._BaseColor.a",new Keyframe(0,1));
        }
        var controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/Animaciones/SeleccionEntrenamiento.controller");
        controller.AddParameter("Focused",AnimatorControllerParameterType.Bool);
        var idle=controller.AddMotion(normal);var selected=controller.AddMotion(focus);
        var enter=idle.AddTransition(selected); enter.hasExitTime=false;enter.duration=.12f;enter.AddCondition(AnimatorConditionMode.If,0,"Focused");
        var exit=selected.AddTransition(idle);exit.hasExitTime=false;exit.duration=.12f;exit.AddCondition(AnimatorConditionMode.IfNot,0,"Focused");
        foreach(var button in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TrainingModeButton>(true))) {
            var animator=button.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            var so=new SerializedObject(button);so.FindProperty("animator").objectReferenceValue=animator;so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);
    }
}
