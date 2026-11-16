using UnityEngine;

namespace Steria.WaterThunder
{
    public static class WaterThunderMotion
    {
        public const float HitTime=.190f, ContactPath=.72f, Duration=1.04f;
        static readonly float[] Times={0,.08f,.16f,.19f,.215f,.25f,.32f};
        static readonly float[] Values={0,.075f,.48f,.72f,.755f,.78f,1};
        static readonly float[] Slopes={0,2,7.3f,3.5f,.7f,1,4.8f};
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
            float s=1-t;
            return s*s*s*new Vector3(-.85f,3.65f,0)+3*s*s*t*new Vector3(2.65f,3.65f,0)+
                3*s*t*t*new Vector3(3.0f,.9f,0)+t*t*t*new Vector3(.8f,.18f,0);
        }
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
        public static Vector4 EvaporationFrame {get{return new Vector4(.65f,1.95f,3.7f,3.85f);}}
        public static float EvaporationProgress(float time){return Span(.36f,.94f,time);}
        public static float EvaporationSpread(float time){return 1+.38f*EvaporationProgress(time);}
        public static float EvaporationLift(float time){return .34f*EvaporationProgress(time);}
    }
}
