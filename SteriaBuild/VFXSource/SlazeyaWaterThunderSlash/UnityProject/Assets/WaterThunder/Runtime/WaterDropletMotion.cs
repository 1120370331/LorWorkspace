using UnityEngine;

namespace Steria.WaterThunder
{
    /// <summary>Scrubbable drag/gravity and damped capillary stretch, shared by wake and hit water.</summary>
    public static class WaterDropletMotion
    {
        public static float Seed(int index)
        {
            uint x=(uint)(index+19037)*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;
            return ((x>>22)^x)/4294967295f;
        }
        public static void Flight(Vector3 initial,float age,float seed,out Vector3 offset,out Vector3 velocity)
        {
            float t=Mathf.Max(0,age),drag=1.2f+seed*1.1f,e=Mathf.Exp(-drag*t),travel=(1-e)/drag;
            var gravity=new Vector3(0,-5.6f,0);
            offset=initial*travel+gravity*(t/drag-travel/drag);
            velocity=initial*e+gravity*travel;
        }
        public static float Stretch(float initialSpeed,float age,float seed,bool ligament)
        {
            float t=Mathf.Max(0,age);
            float baseStretch=1+Mathf.Min(initialSpeed*.24f,1.3f)*Mathf.Exp(-t*8);
            float capillary=Mathf.Sin(t*(28+seed*19)+seed*17)*Mathf.Exp(-t*7)*.11f;
            return Mathf.Max(.88f,baseStretch+capillary+(ligament?1.7f*Mathf.Exp(-t*7):0));
        }
        public static Color32 DataColor(float seed,float age,float life,bool ligament,float alpha)
        {
            return new Color32((byte)Mathf.RoundToInt(seed*255),(byte)Mathf.RoundToInt(Mathf.Clamp01(age/life)*255),ligament?(byte)230:(byte)0,(byte)Mathf.RoundToInt(Mathf.Clamp01(alpha)*255));
        }
    }
}
