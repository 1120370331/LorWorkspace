using UnityEngine;

namespace Steria.WaterThunder
{
    public static class WaterThunderMotion
    {
        public const float HitTime=.190f, MainPathFraction=.86f, ContactPath=.72f*MainPathFraction, Duration=1.04f;
        static readonly float[] Times={0,.08f,.16f,.19f,.215f,.25f,.32f};
        static readonly float[] Values={0,.075f*MainPathFraction,.48f*MainPathFraction,ContactPath,.755f*MainPathFraction,.78f*MainPathFraction,1};
        static readonly float[] Slopes={0,2*MainPathFraction,7.3f*MainPathFraction,3.5f*MainPathFraction,.7f*MainPathFraction,MainPathFraction,5.8f};
        public const float FrontExtraDegrees=45, TailRetreatDegrees=45;
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
        public static Vector3 BaseCurve(float t)
        {
            float s=1-t;
            return s*s*s*new Vector3(-.85f,3.65f,0)+3*s*s*t*new Vector3(2.65f,3.65f,0)+
                3*s*t*t*new Vector3(3.0f,.9f,0)+t*t*t*new Vector3(.8f,.18f,0);
        }
        public static Vector3 Curve(float t)
        {
            float q=Mathf.Clamp01(t)/MainPathFraction;
            if(q<=1)return BaseCurve(q);
            float u=(q-1)/(1/MainPathFraction-1),theta=Mathf.Atan2(-.72f,-2.2f),a=FrontExtraDegrees*Mathf.Deg2Rad*u;
            const float radius=1.25f;
            return BaseCurve(1)+new Vector3(radius*(Mathf.Sin(theta+a)-Mathf.Sin(theta)),-radius*(Mathf.Cos(theta+a)-Mathf.Cos(theta)),0);
        }
        static float FindTailRetreat()
        {
            var start=(Vector2)BaseCurve(0)-ArcCenter;float goal=Mathf.Atan2(start.y,start.x)*Mathf.Rad2Deg-TailRetreatDegrees;
            float lo=0,hi=.6f;
            for(int i=0;i<24;i++){float mid=(lo+hi)*.5f;var d=(Vector2)BaseCurve(mid)-ArcCenter;if(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg>goal)lo=mid;else hi=mid;}
            return (lo+hi)*.5f*MainPathFraction;
        }
        public static float TailRetreat(float time){return TailRetreatLimit*Mathf.SmoothStep(0,1,Span(.215f,.48f,time));}
        public static Vector3 AuthoringOffset {get{return Vector3.up*(-Curve(1).y+.12f);}}
        public static Vector3 GameArtOffset(float scale){return new Vector3(.8f*scale,(-Curve(1).y+.15f)*scale,0);}
        public static float TimeAtPath(float path)
        {
            float a=0,b=.32f;for(int i=0;i<22;i++){float m=(a+b)*.5f;if(Progress(m)<path)a=m;else b=m;}return (a+b)*.5f;
        }
        public static Vector3 Normal(float t)
        {
            var d=Curve(Mathf.Min(1,t+.001f))-Curve(Mathf.Max(0,t-.001f));
            return new Vector3(d.y,-d.x,0).normalized;
        }
        public static float Width(float t){return .78f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.7f)*(.3f+t);}
        public static Vector3 Contact {get{return Curve(ContactPath);}}
        public static Vector4 EvaporationFrame {get{return new Vector4(.65f,1.70f,3.7f,4.8f);}}
        public static float EvaporationProgress(float time){return Span(.36f,.94f,time);}
        public static float EvaporationSpread(float time){return 1+.38f*EvaporationProgress(time);}
        public static float EvaporationLift(float time){return .34f*EvaporationProgress(time);}
    }
}
