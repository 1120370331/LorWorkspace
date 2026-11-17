using UnityEngine;

namespace Steria.WaterThunder
{
    public static class WaterThunderMotion
    {
        public const float HitTime=.190f, MainPathFraction=.86f, ContactPath=.72f*MainPathFraction, Duration=1.04f;
        static readonly float[] Times={0,.08f,.16f,.19f,.215f,.25f,.32f};
        static readonly float[] Values={0,.075f*MainPathFraction,.48f*MainPathFraction,ContactPath,.755f*MainPathFraction,.78f*MainPathFraction,1};
        static readonly float[] Slopes={0,2*MainPathFraction,7.3f*MainPathFraction,3.5f*MainPathFraction,.7f*MainPathFraction,MainPathFraction,5.8f};
        public const float ArcRadius=1.95f, TailRetreatDegrees=45;
        public const float RetreatStart=.175f, RetreatEnd=.340f, WaterSweepWidth=.42f;
        public static readonly Vector2 ArcCenter=new Vector2(.65f,1.95f);
        public static readonly float TailRetreatLimit=FindTailRetreat();
        public static float Span(float a,float b,float t){return Mathf.Clamp01((t-a)/(b-a));}
        public static float Progress(float t)
        {
            if(t<=0)return 0;if(t>=.32f)return 1;
            for(int i=0;i<Times.Length-1;i++)if(t<=Times[i+1])
            {
                float d=Times[i+1]-Times[i],q=(t-Times[i])/d,q2=q*q,q3=q2*q;
                return Mathf.Clamp01((2*q3-3*q2+1)*Values[i]+(q3-2*q2+q)*d*Slopes[i]+(-2*q3+3*q2)*Values[i+1]+(q3-q2)*d*Slopes[i+1]);
            }
            return 1;
        }
        public static Vector3 Curve(float t)
        {
            // One center for the complete blade: no appended circle and no hook at the lower mouth.
            float angle=Mathf.Lerp(90,-90,Mathf.Clamp01(t))*Mathf.Deg2Rad;
            return new Vector3(ArcCenter.x+ArcRadius*Mathf.Cos(angle),ArcCenter.y+ArcRadius*Mathf.Sin(angle),0);
        }
        public static Vector3 BaseCurve(float t){return Curve(t*MainPathFraction);}
        static float FindTailRetreat(){return TailRetreatDegrees/180f;}
        public static float TailRetreat(float time){return TailRetreatLimit*Mathf.SmoothStep(0,1,Span(RetreatStart,RetreatEnd,time));}
        public static Vector3 AuthoringOffset {get{return Vector3.up*(-Curve(1).y+.30f);}}
        public static Vector3 GameArtOffset(float scale){return new Vector3(.8f*scale,(-Curve(1).y+.33f)*scale,0);}
        public static float TimeAtPath(float path)
        {
            float a=0,b=.32f;for(int i=0;i<22;i++){float m=(a+b)*.5f;if(Progress(m)<path)a=m;else b=m;}return (a+b)*.5f;
        }
        public static Vector3 Normal(float t)
        {
            var d=Curve(Mathf.Min(1,t+.001f))-Curve(Mathf.Max(0,t-.001f));
            return new Vector3(d.y,-d.x,0).normalized;
        }
        public static float Width(float t){return Mathf.Lerp(.32f,.90f,Mathf.SmoothStep(0,1,Span(.02f,.62f,t)))*(1-Mathf.SmoothStep(0,1,Span(.97f,1,t)));}
        public static void WaterVertex(float path,float across,out Vector3 position,out Vector3 tailOffset)
        {
            float width=Width(path),tail=Mathf.Pow(1-Span(0,.65f,path),1.4f);
            var center=Curve(path);var normal=Normal(path);
            float inset=.028f+across*width*(1+.035f*Mathf.Sin(path*57+across*9));
            var basePoint=center+normal*inset;
            float behind=tail*(.10f+.28f*across)/(Mathf.PI*ArcRadius),tailPath=Mathf.Clamp01(path-behind);
            var tailPoint=Curve(tailPath)+Normal(tailPath)*(.028f+across*(width+.60f*tail));
            tailOffset=tailPoint-basePoint;position=basePoint+tailOffset;
            position.z=.012f*Mathf.Sin(path*10)*Mathf.Sin(across*Mathf.PI);
        }
        static Vector3 WaterCorner(float path,float across,float time)
        {
            Vector3 position,tail;WaterVertex(path,across,out position,out tail);
            float growth=Mathf.SmoothStep(0,1,Span(.12f,.37f,Progress(time)));
            float flutter=(Mathf.Sin(path*23+across*17-time*9)+.45f*Mathf.Sin(path*41-across*9-time*6))*.16f;
            float near=1-Mathf.SmoothStep(0,1,Span(TailRetreat(time)+.06f,TailRetreat(time)+.30f,path));
            position+=tail*(growth-1)+tail*flutter*near*growth;
            var radial=(Vector2)position-ArcCenter;float limit=ArcRadius-.028f;
            if(radial.magnitude>limit){var point=ArcCenter+radial.normalized*limit;position.x=point.x;position.y=point.y;}
            return position;
        }
        public static float WaterVisibleInnerV(float path,float time)
        {
            float passed=Mathf.Max(0,Progress(time)-path);
            float spread=Mathf.SmoothStep(0,1,Span(.015f,.18f,passed));
            float brushU=(path-Progress(time))/WaterSweepWidth+.92f;
            float brushEdge=brushU>=0&&brushU<=1?Mathf.Pow(1-brushU,.72f)/.82f:0;
            return Mathf.Clamp(Mathf.Max(.04f+spread*.96f,brushEdge)-.045f,.02f,.92f);
        }
        public static Vector3 WaterSurfacePoint(float path,float across,float time)
        {
            // Interpolate the same 192x14 triangles that the water renderer actually uses.
            float u=Mathf.Clamp01(path)*192,v=Mathf.Clamp01(across)*14;
            int i=Mathf.Min(191,Mathf.FloorToInt(u)),j=Mathf.Min(13,Mathf.FloorToInt(v));float x=u-i,y=v-j;
            var a=WaterCorner(i/192f,j/14f,time);var b=WaterCorner((i+1)/192f,j/14f,time);
            var c=WaterCorner(i/192f,(j+1)/14f,time);var d=WaterCorner((i+1)/192f,(j+1)/14f,time);
            return x+y<=1?a+(b-a)*x+(c-a)*y:d+(c-d)*(1-x)+(b-d)*(1-y);
        }
        public static Vector3 Contact {get{return Curve(ContactPath);}}
        public static Vector4 EvaporationFrame {get{return new Vector4(.65f,1.95f,4.6f,4.8f);}}
        public static float EvaporationProgress(float time){return Span(.36f,.94f,time);}
        public static float EvaporationSpread(float time){return 1+.38f*EvaporationProgress(time);}
        public static float EvaporationLift(float time){return .34f*EvaporationProgress(time);}
    }
}
