// CPU fixture boundary: containers/positions only. All queue methods, moving
// actions, enums and BehaviourActionBase are linked/extracted from game source.
using LOR_DiceSystem;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static float Distance(Vector3 a, Vector3 b) => MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
    }
    public class Transform { public Vector3 localScale = new Vector3(1.5f, 1.5f, 1.5f); }
}
public class BattleUnitView
{
    public CharacterAppearance charAppearance;
    public UnityEngine.Vector3 WorldPosition;
    public UnityEngine.Transform transform = new UnityEngine.Transform();
}
public class CharacterAppearance
{
    public Workshop.WorkshopSkinDataSetter Outfit = new();
    public T GetComponent<T>() where T : class => Outfit as T;
    public object GetCharacterMotion(ActionDetail action) => Outfit.dic.ContainsKey(action) ? this : null;
}
namespace Workshop
{
    public class ClothCustomizeData { public string spritePath; }
    public class WorkshopSkinDataSetter { public Dictionary<ActionDetail, ClothCustomizeData> dic = new(); }
}
public class BattleUnitModel { }
public class FarAreaEffect { }
public class HexagonalMapManager { public float tileSize = 1; }
public class SingletonBehavior<T> where T : new() { public static T Instance { get; } = new T(); }
public class DmgTakenData { public int dmg; }
public class DiceBehaviour { public string ActionScript; }
public class BattleCardBehaviourResult
{
    public List<DmgTakenData> dmgListTaken = new();
    public List<DmgTakenData> breakListTaken = new();
    public DiceBehaviour behaviourRawData;
    public bool isBlockedDice;
}
