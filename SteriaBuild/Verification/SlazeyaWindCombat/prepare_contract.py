"""Prepare unchanged production adapters + native methods; explicitly managed, not Unity."""
import hashlib
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
GEN = HERE / 'Generated'
GEN.mkdir(exist_ok=True)
sources = {}


def method(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


for name in ('DiceAttackEffect_Steria_SlazeyaWind.cs', 'SlazeyaWindParticleAssets.cs'):
    source = ROOT / 'SteriaBuild' / name
    (GEN / name).write_bytes(source.read_bytes())
    sources[str(source.relative_to(ROOT))] = hashlib.sha256(source.read_bytes()).hexdigest()
native = ROOT / 'SteriaBuild/Assembly-CSharp/Battle/DiceAttackEffect/DiceAttackEffect.cs'
(GEN / 'NativeDiceAttackEffect.cs').write_text('using Object = UnityEngine.Object;\n' + native.read_text(encoding='utf-8-sig'), encoding='utf-8')
sources[str(native.relative_to(ROOT))] = hashlib.sha256(native.read_bytes()).hexdigest()
unity = (ROOT / 'SteriaBuild/VFXSource/AnhierTextureCombat/ManagedFixture/UnityContract.cs').read_text(encoding='utf-8-sig')
unity = unity.replace('public string name; public bool destroyed;', 'public string name; public bool destroyed; public int GetInstanceID(){return GetHashCode();}')
unity = unity.replace('public static void Destroy(Object o){if(o!=null)o.destroyed=true;}', '''public static void Destroy(Object o){
if(o==null || o.destroyed)return; o.destroyed=true;
var go=o as GameObject;if(go!=null)foreach(var c in go.components.ToArray()){
foreach(string name in new[]{"OnDisable","OnDestroy"}){var m=c.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(m!=null)m.Invoke(c,null);}}}
public static T Instantiate<T>(T original,Transform parent,bool world)where T:Object {var g=new GameObject(original.name);g.transform.SetParent(parent,world);var prefab=original as GameObject;if(prefab!=null){var renderer=prefab.GetComponent<ParticleSystemRenderer>();if(renderer!=null)g.AddComponent<ParticleSystemRenderer>().sortingOrder=renderer.sortingOrder;foreach(var child in prefab.transform.children)Instantiate(child.gameObject,g.transform,false);}return (T)(Object)g;}
''')
unity = unity.replace('public T[] GetComponentsInChildren<T>() where T:Component=>gameObject.GetComponentsInChildren<T>();', 'public T[] GetComponentsInChildren<T>() where T:Component=>gameObject.GetComponentsInChildren<T>();public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component=>gameObject.GetComponentsInChildren<T>(includeInactive);')
unity = unity.replace('public T[] GetComponentsInChildren<T>() where T:Component=>components.OfType<T>().Concat(transform.children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>())).ToArray();', 'public T[] GetComponentsInChildren<T>() where T:Component=>GetComponentsInChildren<T>(false);public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component=>components.OfType<T>().Concat(transform.children.Where(c=>includeInactive||c.gameObject.activeInHierarchy).SelectMany(c=>c.gameObject.GetComponentsInChildren<T>(includeInactive))).ToArray();')
unity = unity.replace('public static float Distance(Vector3 a,Vector3 b)', 'public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);public static float Distance(Vector3 a,Vector3 b)')
unity = unity.replace('public static class Mathf{', 'public static class Mathf{public const float Rad2Deg=57.295779513f;public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);')
unity = unity.replace('public static void LogError(string s){throw new Exception(s);}', 'public static readonly List<string> Errors=new List<string>();public static void Log(string s){} public static void LogError(string s){Errors.Add(s);}')
# The native pivot API is the existing fixture's verbatim game method. Record provenance.
unity += '''
public class DiceEffectManager {}
namespace UnityEngine {public class ParticleSystemRenderer:Component {public string sortingLayerName="Default";public int sortingOrder;}public class AssetBundle:Object {
public static AssetBundle LoadFromFile(string path){return new AssetBundle();}
public T LoadAsset<T>(string name)where T:Object {var g=new GameObject(name);var main=new GameObject("Main");main.transform.SetParent(g.transform,false);main.AddComponent<ParticleSystemRenderer>();var silk=new GameObject("Silk");silk.transform.SetParent(g.transform,false);silk.AddComponent<ParticleSystemRenderer>().sortingOrder=2;return (T)(Object)g;}}}
'''
(GEN / 'UnityContract.cs').write_text(unity, encoding='utf-8')
harmony = (ROOT / 'SteriaBuild/HarmonyPatches.cs').read_text(encoding='utf-8-sig')
registrations = '\n'.join(line for line in harmony.splitlines() if re.search(r'SteriaCustomEffects\["Steria_SlazeyaWind', line))
assert len(registrations.splitlines()) == 5
prefix = method(harmony, 'public static bool DiceEffectManager_CreateBehaviourEffect_Prefix(')
(GEN / 'ProductionFactory.cs').write_text('using System;using System.Collections.Generic;using Steria;using UnityEngine;public static class ProductionFactory {public static Dictionary<string,Type> SteriaCustomEffects=new Dictionary<string,Type>();public static void Init(){' + registrations + '}\n' + prefix + '}\n', encoding='utf-8')
sources['SteriaBuild/HarmonyPatches.cs'] = hashlib.sha256((ROOT / 'SteriaBuild/HarmonyPatches.cs').read_bytes()).hexdigest()
(HERE / 'candidate-hashes.json').write_text(json.dumps({'kind':'production adapter + extracted native methods managed contract; Unity/driver/AssetBundle APIs are bounded doubles; no GPU claims', 'files':sources}, indent=2) + '\n')
print('Prepared exact production adapter, assets, five factory registrations/prefix; bounded Unity/driver doubles')
