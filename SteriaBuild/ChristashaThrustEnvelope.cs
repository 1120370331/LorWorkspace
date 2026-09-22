using System;
using UnityEngine;

namespace Steria.Vfx
{
    /// <summary>Find the visible exit behind the victim, not just its front hit pivot.</summary>
    public static class ChristashaThrustEnvelope
    {
        public static Vector3 Exit(Vector3 origin, Vector3 contact, Vector3 victimCenter, float victimScale)
        {
            Vector3 delta=contact-origin;
            float distance=(float)Math.Sqrt(delta.x*delta.x+delta.y*delta.y);
            if(distance<.001f)return contact;
            var direction=new Vector3(delta.x/distance,delta.y/distance,0);
            float scale=Math.Max(.1f,Math.Abs(victimScale));
            float centerDistance=(victimCenter.x-origin.x)*direction.x+(contact.y-origin.y)*direction.y;
            float length=Math.Max(distance+2f*scale,centerDistance+3.2f*scale);
            var end=origin+direction*length;end.z=contact.z;return end;
        }
    }
}
