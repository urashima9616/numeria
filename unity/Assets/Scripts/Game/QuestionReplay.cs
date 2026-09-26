using System;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Explicit, repeatable question narration. Never generates or submits a question.</summary>
    public static class QuestionReplay
    {
        public static Button Add(Transform parent, Action<string[]> speak, Func<string[]> lines)
        {
            var button=Ui.Btn(parent,"ReplayQuestion","",18);
            button.targetGraphic.color=Ui.Hex("#f5ebcd");
            var icon=Ui.Node(button.transform,"SpeakerIcon");
            Ui.PlaceCentered(icon,new Vector2(.5f,.64f),Vector2.zero,new Vector2(38,34));
            icon.gameObject.AddComponent<QuestionSpeakerGraphic>().raycastTarget=false;
            var label=Ui.Label(button.transform,"ListenLabel","LISTEN",16,Ui.Ink);
            Ui.PlaceCentered(label.rectTransform,new Vector2(.5f,.18f),Vector2.zero,new Vector2(78,20));
            label.raycastTarget=false;
            button.onClick.AddListener(()=>{if(button.interactable&&button.gameObject.activeInHierarchy)speak?.Invoke(lines());});
            return button;
        }
        public static void Disable(Transform parent)
        {
            foreach(var button in parent.GetComponentsInChildren<Button>())
                if(button.name=="ReplayQuestion")button.interactable=false;
        }
    }

    /// <summary>Font-independent speaker symbol, constructed from convex pieces.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class QuestionSpeakerGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();float scale=Mathf.Min(rectTransform.rect.width/44,rectTransform.rect.height/40);
            void Poly(params Vector2[] points)
            {
                int start=mesh.currentVertCount;
                foreach(var point in points)mesh.AddVert(point*scale,Ui.Ink,Vector2.zero);
                for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);
            }
            Poly(new Vector2(-19,-6),new Vector2(-11,-6),new Vector2(-11,6),new Vector2(-19,6));
            Poly(new Vector2(-11,-6),new Vector2(0,-14),new Vector2(0,14),new Vector2(-11,6));
            foreach(float radius in new[]{10f,18f})for(int i=0;i<8;i++)
            {
                float a=(-55+i*110f/8)*Mathf.Deg2Rad,b=(-55+(i+1)*110f/8)*Mathf.Deg2Rad;
                Vector2 At(float angle,float r)=>new Vector2(1+Mathf.Cos(angle)*r,Mathf.Sin(angle)*r);
                Poly(At(a,radius),At(b,radius),At(b,radius+3),At(a,radius+3));
            }
        }
    }
}
