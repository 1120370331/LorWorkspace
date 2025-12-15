using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Spine;
using UnityEngine;

namespace BaseMod
{
	// Token: 0x02000062 RID: 98
	public class SkeletonJSON_new
	{
		// Token: 0x060001AE RID: 430 RVA: 0x00010644 File Offset: 0x0000E844
		public static void ReadAnimation_new(SkeletonJson __instance, Dictionary<string, object> map, string name, SkeletonData skeletonData)
		{
			float num = 1f;
			ExposedList<Timeline> exposedList = new ExposedList<Timeline>();
			float num2 = 0f;
			if (map.ContainsKey("slots"))
			{
				foreach (KeyValuePair<string, object> keyValuePair in ((Dictionary<string, object>)map["slots"]))
				{
					string key = keyValuePair.Key;
					int slotIndex = skeletonData.FindSlotIndex(key);
					foreach (KeyValuePair<string, object> keyValuePair2 in ((Dictionary<string, object>)keyValuePair.Value))
					{
						List<object> list = (List<object>)keyValuePair2.Value;
						string key2 = keyValuePair2.Key;
						if (key2 == "attachment")
						{
							AttachmentTimeline attachmentTimeline = new AttachmentTimeline(list.Count)
							{
								SlotIndex = slotIndex
							};
							int num3 = 0;
							foreach (object obj in list)
							{
								Dictionary<string, object> dictionary = (Dictionary<string, object>)obj;
								float num4 = (float)dictionary["time"];
								attachmentTimeline.SetFrame(num3++, num4, (string)dictionary["name"]);
							}
							exposedList.Add(attachmentTimeline);
							num2 = Math.Max(num2, attachmentTimeline.Frames[attachmentTimeline.FrameCount - 1]);
						}
						else if (key2 == "color")
						{
							ColorTimeline colorTimeline = new ColorTimeline(list.Count)
							{
								SlotIndex = slotIndex
							};
							int num5 = 0;
							foreach (object obj2 in list)
							{
								Dictionary<string, object> dictionary2 = (Dictionary<string, object>)obj2;
								float num6 = (float)dictionary2["time"];
								string hexString = (string)dictionary2["color"];
								colorTimeline.SetFrame(num5, num6, SkeletonJSON_new.ToColor(hexString, 0, 8), SkeletonJSON_new.ToColor(hexString, 1, 8), SkeletonJSON_new.ToColor(hexString, 2, 8), SkeletonJSON_new.ToColor(hexString, 3, 8));
								SkeletonJSON_new.ReadCurve(dictionary2, colorTimeline, num5);
								num5++;
							}
							exposedList.Add(colorTimeline);
							num2 = Math.Max(num2, colorTimeline.Frames[(colorTimeline.FrameCount - 1) * 5]);
						}
						else
						{
							if (!(key2 == "twoColor"))
							{
								throw new Exception(string.Concat(new string[]
								{
									"Invalid timeline type for a slot: ",
									key2,
									" (",
									key,
									")"
								}));
							}
							TwoColorTimeline twoColorTimeline = new TwoColorTimeline(list.Count)
							{
								SlotIndex = slotIndex
							};
							int num7 = 0;
							foreach (object obj3 in list)
							{
								Dictionary<string, object> dictionary3 = (Dictionary<string, object>)obj3;
								float num8 = (float)dictionary3["time"];
								string hexString2 = (string)dictionary3["light"];
								string hexString3 = (string)dictionary3["dark"];
								twoColorTimeline.SetFrame(num7, num8, SkeletonJSON_new.ToColor(hexString2, 0, 8), SkeletonJSON_new.ToColor(hexString2, 1, 8), SkeletonJSON_new.ToColor(hexString2, 2, 8), SkeletonJSON_new.ToColor(hexString2, 3, 8), SkeletonJSON_new.ToColor(hexString3, 0, 6), SkeletonJSON_new.ToColor(hexString3, 1, 6), SkeletonJSON_new.ToColor(hexString3, 2, 6));
								SkeletonJSON_new.ReadCurve(dictionary3, twoColorTimeline, num7);
								num7++;
							}
							exposedList.Add(twoColorTimeline);
							num2 = Math.Max(num2, twoColorTimeline.Frames[(twoColorTimeline.FrameCount - 1) * 8]);
						}
					}
				}
			}
			if (map.ContainsKey("bones"))
			{
				foreach (KeyValuePair<string, object> keyValuePair3 in ((Dictionary<string, object>)map["bones"]))
				{
					string key3 = keyValuePair3.Key;
					int num9 = skeletonData.FindBoneIndex(key3);
					if (num9 == -1)
					{
						throw new Exception("Bone not found: " + key3);
					}
					foreach (KeyValuePair<string, object> keyValuePair4 in ((Dictionary<string, object>)keyValuePair3.Value))
					{
						List<object> list2 = (List<object>)keyValuePair4.Value;
						string key4 = keyValuePair4.Key;
						if (key4 == "rotate")
						{
							RotateTimeline rotateTimeline = new RotateTimeline(list2.Count)
							{
								BoneIndex = num9
							};
							int num10 = 0;
							foreach (object obj4 in list2)
							{
								Dictionary<string, object> dictionary4 = (Dictionary<string, object>)obj4;
								rotateTimeline.SetFrame(num10, (float)dictionary4["time"], (float)dictionary4["angle"]);
								SkeletonJSON_new.ReadCurve(dictionary4, rotateTimeline, num10);
								num10++;
							}
							exposedList.Add(rotateTimeline);
							num2 = Math.Max(num2, rotateTimeline.Frames[(rotateTimeline.FrameCount - 1) * 2]);
						}
						else
						{
							if (!(key4 == "translate") && !(key4 == "scale") && !(key4 == "shear"))
							{
								throw new Exception(string.Concat(new string[]
								{
									"Invalid timeline type for a bone: ",
									key4,
									" (",
									key3,
									")"
								}));
							}
							float num11 = 1f;
							TranslateTimeline translateTimeline;
							if (key4 == "scale")
							{
								translateTimeline = new ScaleTimeline(list2.Count);
							}
							else if (key4 == "shear")
							{
								translateTimeline = new ShearTimeline(list2.Count);
							}
							else
							{
								translateTimeline = new TranslateTimeline(list2.Count);
								num11 = num;
							}
							translateTimeline.BoneIndex = num9;
							int num12 = 0;
							foreach (object obj5 in list2)
							{
								Dictionary<string, object> dictionary5 = (Dictionary<string, object>)obj5;
								float num13 = (float)dictionary5["time"];
								float @float = SkeletonJSON_new.GetFloat(dictionary5, "x", 0f);
								float float2 = SkeletonJSON_new.GetFloat(dictionary5, "y", 0f);
								translateTimeline.SetFrame(num12, num13, @float * num11, float2 * num11);
								SkeletonJSON_new.ReadCurve(dictionary5, translateTimeline, num12);
								num12++;
							}
							exposedList.Add(translateTimeline);
							num2 = Math.Max(num2, translateTimeline.Frames[(translateTimeline.FrameCount - 1) * 3]);
						}
					}
				}
			}
			if (map.ContainsKey("ik"))
			{
				foreach (KeyValuePair<string, object> keyValuePair5 in ((Dictionary<string, object>)map["ik"]))
				{
					IkConstraintData ikConstraintData = skeletonData.FindIkConstraint(keyValuePair5.Key);
					List<object> list3 = (List<object>)keyValuePair5.Value;
					IkConstraintTimeline ikConstraintTimeline = new IkConstraintTimeline(list3.Count)
					{
						IkConstraintIndex = skeletonData.IkConstraints.IndexOf(ikConstraintData)
					};
					int num14 = 0;
					foreach (object obj6 in list3)
					{
						Dictionary<string, object> dictionary6 = (Dictionary<string, object>)obj6;
						float num15 = (float)dictionary6["time"];
						float float3 = SkeletonJSON_new.GetFloat(dictionary6, "mix", 1f);
						bool boolean = SkeletonJSON_new.GetBoolean(dictionary6, "bendPositive", true);
						ikConstraintTimeline.SetFrame(num14, num15, float3, 0f, (!boolean) ? -1 : 1, false, false);
						SkeletonJSON_new.ReadCurve(dictionary6, ikConstraintTimeline, num14);
						num14++;
					}
					exposedList.Add(ikConstraintTimeline);
					num2 = Math.Max(num2, ikConstraintTimeline.Frames[(ikConstraintTimeline.FrameCount - 1) * 3]);
				}
			}
			if (map.ContainsKey("transform"))
			{
				foreach (KeyValuePair<string, object> keyValuePair6 in ((Dictionary<string, object>)map["transform"]))
				{
					TransformConstraintData transformConstraintData = skeletonData.FindTransformConstraint(keyValuePair6.Key);
					List<object> list4 = (List<object>)keyValuePair6.Value;
					TransformConstraintTimeline transformConstraintTimeline = new TransformConstraintTimeline(list4.Count)
					{
						TransformConstraintIndex = skeletonData.TransformConstraints.IndexOf(transformConstraintData)
					};
					int num16 = 0;
					foreach (object obj7 in list4)
					{
						Dictionary<string, object> dictionary7 = (Dictionary<string, object>)obj7;
						float num17 = (float)dictionary7["time"];
						float float4 = SkeletonJSON_new.GetFloat(dictionary7, "rotateMix", 1f);
						float float5 = SkeletonJSON_new.GetFloat(dictionary7, "translateMix", 1f);
						float float6 = SkeletonJSON_new.GetFloat(dictionary7, "scaleMix", 1f);
						float float7 = SkeletonJSON_new.GetFloat(dictionary7, "shearMix", 1f);
						transformConstraintTimeline.SetFrame(num16, num17, float4, float5, float6, float7);
						SkeletonJSON_new.ReadCurve(dictionary7, transformConstraintTimeline, num16);
						num16++;
					}
					exposedList.Add(transformConstraintTimeline);
					num2 = Math.Max(num2, transformConstraintTimeline.Frames[(transformConstraintTimeline.FrameCount - 1) * 5]);
				}
			}
			if (map.ContainsKey("paths"))
			{
				foreach (KeyValuePair<string, object> keyValuePair7 in ((Dictionary<string, object>)map["paths"]))
				{
					int num18 = skeletonData.FindPathConstraintIndex(keyValuePair7.Key);
					if (num18 == -1)
					{
						throw new Exception("Path constraint not found: " + keyValuePair7.Key);
					}
					PathConstraintData pathConstraintData = skeletonData.PathConstraints.Items[num18];
					foreach (KeyValuePair<string, object> keyValuePair8 in ((Dictionary<string, object>)keyValuePair7.Value))
					{
						List<object> list5 = (List<object>)keyValuePair8.Value;
						string key5 = keyValuePair8.Key;
						if (key5 == "position" || key5 == "spacing")
						{
							float num19 = 1f;
							PathConstraintPositionTimeline pathConstraintPositionTimeline;
							if (key5 == "spacing")
							{
								pathConstraintPositionTimeline = new PathConstraintSpacingTimeline(list5.Count);
								if (pathConstraintData.SpacingMode == null || pathConstraintData.SpacingMode == 1)
								{
									num19 = num;
								}
							}
							else
							{
								pathConstraintPositionTimeline = new PathConstraintPositionTimeline(list5.Count);
								if (pathConstraintData.PositionMode == null)
								{
									num19 = num;
								}
							}
							pathConstraintPositionTimeline.PathConstraintIndex = num18;
							int num20 = 0;
							foreach (object obj8 in list5)
							{
								Dictionary<string, object> dictionary8 = (Dictionary<string, object>)obj8;
								pathConstraintPositionTimeline.SetFrame(num20, (float)dictionary8["time"], SkeletonJSON_new.GetFloat(dictionary8, key5, 0f) * num19);
								SkeletonJSON_new.ReadCurve(dictionary8, pathConstraintPositionTimeline, num20);
								num20++;
							}
							exposedList.Add(pathConstraintPositionTimeline);
							num2 = Math.Max(num2, pathConstraintPositionTimeline.Frames[(pathConstraintPositionTimeline.FrameCount - 1) * 2]);
						}
						else if (key5 == "mix")
						{
							PathConstraintMixTimeline pathConstraintMixTimeline = new PathConstraintMixTimeline(list5.Count)
							{
								PathConstraintIndex = num18
							};
							int num21 = 0;
							foreach (object obj9 in list5)
							{
								Dictionary<string, object> dictionary9 = (Dictionary<string, object>)obj9;
								pathConstraintMixTimeline.SetFrame(num21, (float)dictionary9["time"], SkeletonJSON_new.GetFloat(dictionary9, "rotateMix", 1f), SkeletonJSON_new.GetFloat(dictionary9, "translateMix", 1f));
								SkeletonJSON_new.ReadCurve(dictionary9, pathConstraintMixTimeline, num21);
								num21++;
							}
							exposedList.Add(pathConstraintMixTimeline);
							num2 = Math.Max(num2, pathConstraintMixTimeline.Frames[(pathConstraintMixTimeline.FrameCount - 1) * 3]);
						}
					}
				}
			}
			if (map.ContainsKey("deform"))
			{
				foreach (KeyValuePair<string, object> keyValuePair9 in ((Dictionary<string, object>)map["deform"]))
				{
					Skin skin = skeletonData.FindSkin(keyValuePair9.Key);
					foreach (KeyValuePair<string, object> keyValuePair10 in ((Dictionary<string, object>)keyValuePair9.Value))
					{
						int num22 = skeletonData.FindSlotIndex(keyValuePair10.Key);
						if (num22 == -1)
						{
							throw new Exception("Slot not found: " + keyValuePair10.Key);
						}
						foreach (KeyValuePair<string, object> keyValuePair11 in ((Dictionary<string, object>)keyValuePair10.Value))
						{
							List<object> list6 = (List<object>)keyValuePair11.Value;
							VertexAttachment vertexAttachment = (VertexAttachment)skin.GetAttachment(num22, keyValuePair11.Key);
							if (vertexAttachment == null)
							{
								throw new Exception("Deform attachment not found: " + keyValuePair11.Key);
							}
							bool flag = vertexAttachment.Bones != null;
							float[] vertices = vertexAttachment.Vertices;
							int num23 = (!flag) ? vertices.Length : (vertices.Length / 3 * 2);
							DeformTimeline deformTimeline = new DeformTimeline(list6.Count)
							{
								SlotIndex = num22,
								Attachment = vertexAttachment
							};
							int num24 = 0;
							foreach (object obj10 in list6)
							{
								Dictionary<string, object> dictionary10 = (Dictionary<string, object>)obj10;
								float[] array;
								if (!dictionary10.ContainsKey("vertices"))
								{
									array = ((!flag) ? vertices : new float[num23]);
								}
								else
								{
									array = new float[num23];
									int @int = SkeletonJSON_new.GetInt(dictionary10, "offset", 0);
									float[] floatArray = SkeletonJSON_new.GetFloatArray(dictionary10, "vertices", 1f);
									Array.Copy(floatArray, 0, array, @int, floatArray.Length);
									if (num != 1f)
									{
										int i = @int;
										int num25 = i + floatArray.Length;
										while (i < num25)
										{
											array[i] *= num;
											i++;
										}
									}
									if (!flag)
									{
										for (int j = 0; j < num23; j++)
										{
											array[j] += vertices[j];
										}
									}
								}
								deformTimeline.SetFrame(num24, (float)dictionary10["time"], array);
								SkeletonJSON_new.ReadCurve(dictionary10, deformTimeline, num24);
								num24++;
							}
							exposedList.Add(deformTimeline);
							num2 = Math.Max(num2, deformTimeline.Frames[deformTimeline.FrameCount - 1]);
						}
					}
				}
			}
			if (map.ContainsKey("drawOrder") || map.ContainsKey("draworder"))
			{
				List<object> list7 = (List<object>)map[(!map.ContainsKey("drawOrder")) ? "draworder" : "drawOrder"];
				DrawOrderTimeline drawOrderTimeline = new DrawOrderTimeline(list7.Count);
				int count = skeletonData.Slots.Count;
				int num26 = 0;
				foreach (object obj11 in list7)
				{
					Dictionary<string, object> dictionary11 = (Dictionary<string, object>)obj11;
					int[] array2 = null;
					if (dictionary11.ContainsKey("offsets"))
					{
						array2 = new int[count];
						for (int k = count - 1; k >= 0; k--)
						{
							array2[k] = -1;
						}
						List<object> list8 = (List<object>)dictionary11["offsets"];
						int[] array3 = new int[count - list8.Count];
						int num27 = 0;
						int num28 = 0;
						using (List<object>.Enumerator enumerator5 = list8.GetEnumerator())
						{
							while (enumerator5.MoveNext())
							{
								object obj12 = enumerator5.Current;
								Dictionary<string, object> dictionary12 = (Dictionary<string, object>)obj12;
								int num29 = skeletonData.FindSlotIndex((string)dictionary12["slot"]);
								if (num29 == -1)
								{
									string str = "Slot not found: ";
									object obj13 = dictionary12["slot"];
									throw new Exception(str + ((obj13 != null) ? obj13.ToString() : null));
								}
								while (num27 != num29)
								{
									array3[num28++] = num27++;
								}
								int num30 = num27 + (int)((float)dictionary12["offset"]);
								array2[num30] = num27++;
							}
							goto IL_105F;
						}
						goto IL_104E;
						IL_105F:
						if (num27 >= count)
						{
							for (int l = count - 1; l >= 0; l--)
							{
								if (array2[l] == -1)
								{
									array2[l] = array3[--num28];
								}
							}
							goto IL_108F;
						}
						IL_104E:
						array3[num28++] = num27++;
						goto IL_105F;
					}
					IL_108F:
					drawOrderTimeline.SetFrame(num26++, (float)dictionary11["time"], array2);
				}
				exposedList.Add(drawOrderTimeline);
				num2 = Math.Max(num2, drawOrderTimeline.Frames[drawOrderTimeline.FrameCount - 1]);
			}
			if (map.ContainsKey("events"))
			{
				List<object> list9 = (List<object>)map["events"];
				EventTimeline eventTimeline = new EventTimeline(list9.Count);
				int num31 = 0;
				foreach (object obj14 in list9)
				{
					Dictionary<string, object> dictionary13 = (Dictionary<string, object>)obj14;
					EventData eventData = skeletonData.FindEvent((string)dictionary13["name"]);
					if (eventData == null)
					{
						string str2 = "Event not found: ";
						object obj15 = dictionary13["name"];
						throw new Exception(str2 + ((obj15 != null) ? obj15.ToString() : null));
					}
					Event @event = new Event((float)dictionary13["time"], eventData)
					{
						Int = SkeletonJSON_new.GetInt(dictionary13, "int", eventData.Int),
						Float = SkeletonJSON_new.GetFloat(dictionary13, "float", eventData.Float),
						String = SkeletonJSON_new.GetString(dictionary13, "string", eventData.String)
					};
					eventTimeline.SetFrame(num31++, @event);
				}
				exposedList.Add(eventTimeline);
				num2 = Math.Max(num2, eventTimeline.Frames[eventTimeline.FrameCount - 1]);
			}
			exposedList.TrimExcess();
			skeletonData.Animations.Add(new Animation(name, exposedList, num2));
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00011AE4 File Offset: 0x0000FCE4
		public static void ReadAnimation(SkeletonJson __instance, Dictionary<string, object> map, string name, SkeletonData skeletonData)
		{
			float scale = __instance.Scale;
			ExposedList<Timeline> exposedList = new ExposedList<Timeline>();
			float num = 0f;
			if (map.ContainsKey("slots"))
			{
				foreach (KeyValuePair<string, object> keyValuePair in ((Dictionary<string, object>)map["slots"]))
				{
					string key = keyValuePair.Key;
					int slotIndex = skeletonData.FindSlotIndex(key);
					foreach (KeyValuePair<string, object> keyValuePair2 in ((Dictionary<string, object>)keyValuePair.Value))
					{
						List<object> list = (List<object>)keyValuePair2.Value;
						string key2 = keyValuePair2.Key;
						if (key2 == "attachment")
						{
							AttachmentTimeline attachmentTimeline = new AttachmentTimeline(list.Count)
							{
								SlotIndex = slotIndex
							};
							int num2 = 0;
							foreach (object obj in list)
							{
								Dictionary<string, object> dictionary = (Dictionary<string, object>)obj;
								float @float = SkeletonJSON_new.GetFloat(dictionary, "time", 0f);
								attachmentTimeline.SetFrame(num2++, @float, (string)dictionary["name"]);
							}
							exposedList.Add(attachmentTimeline);
							num = Math.Max(num, attachmentTimeline.Frames[attachmentTimeline.FrameCount - 1]);
						}
						else if (key2 == "color")
						{
							ColorTimeline colorTimeline = new ColorTimeline(list.Count)
							{
								SlotIndex = slotIndex
							};
							int num3 = 0;
							foreach (object obj2 in list)
							{
								Dictionary<string, object> dictionary2 = (Dictionary<string, object>)obj2;
								float float2 = SkeletonJSON_new.GetFloat(dictionary2, "time", 0f);
								string hexString = (string)dictionary2["color"];
								colorTimeline.SetFrame(num3, float2, SkeletonJSON_new.ToColor(hexString, 0, 8), SkeletonJSON_new.ToColor(hexString, 1, 8), SkeletonJSON_new.ToColor(hexString, 2, 8), SkeletonJSON_new.ToColor(hexString, 3, 8));
								SkeletonJSON_new.ReadCurve(dictionary2, colorTimeline, num3);
								num3++;
							}
							exposedList.Add(colorTimeline);
							num = Math.Max(num, colorTimeline.Frames[(colorTimeline.FrameCount - 1) * 5]);
						}
						else
						{
							if (!(key2 == "twoColor"))
							{
								throw new Exception(string.Concat(new string[]
								{
									"Invalid timeline type for a slot: ",
									key2,
									" (",
									key,
									")"
								}));
							}
							TwoColorTimeline twoColorTimeline = new TwoColorTimeline(list.Count)
							{
								SlotIndex = slotIndex
							};
							int num4 = 0;
							foreach (object obj3 in list)
							{
								Dictionary<string, object> dictionary3 = (Dictionary<string, object>)obj3;
								float float3 = SkeletonJSON_new.GetFloat(dictionary3, "time", 0f);
								string hexString2 = (string)dictionary3["light"];
								string hexString3 = (string)dictionary3["dark"];
								twoColorTimeline.SetFrame(num4, float3, SkeletonJSON_new.ToColor(hexString2, 0, 8), SkeletonJSON_new.ToColor(hexString2, 1, 8), SkeletonJSON_new.ToColor(hexString2, 2, 8), SkeletonJSON_new.ToColor(hexString2, 3, 8), SkeletonJSON_new.ToColor(hexString3, 0, 6), SkeletonJSON_new.ToColor(hexString3, 1, 6), SkeletonJSON_new.ToColor(hexString3, 2, 6));
								SkeletonJSON_new.ReadCurve(dictionary3, twoColorTimeline, num4);
								num4++;
							}
							exposedList.Add(twoColorTimeline);
							num = Math.Max(num, twoColorTimeline.Frames[(twoColorTimeline.FrameCount - 1) * 8]);
						}
					}
				}
			}
			if (map.ContainsKey("bones"))
			{
				foreach (KeyValuePair<string, object> keyValuePair3 in ((Dictionary<string, object>)map["bones"]))
				{
					string key3 = keyValuePair3.Key;
					int num5 = skeletonData.FindBoneIndex(key3);
					if (num5 == -1)
					{
						throw new Exception("Bone not found: " + key3);
					}
					foreach (KeyValuePair<string, object> keyValuePair4 in ((Dictionary<string, object>)keyValuePair3.Value))
					{
						List<object> list2 = (List<object>)keyValuePair4.Value;
						string key4 = keyValuePair4.Key;
						key4 == "rotate";
						if (key4 == "rotate")
						{
							RotateTimeline rotateTimeline = new RotateTimeline(list2.Count)
							{
								BoneIndex = num5
							};
							int num6 = 0;
							foreach (object obj4 in list2)
							{
								Dictionary<string, object> dictionary4 = (Dictionary<string, object>)obj4;
								rotateTimeline.SetFrame(num6, SkeletonJSON_new.GetFloat(dictionary4, "time", 0f), SkeletonJSON_new.GetFloat(dictionary4, "angle", 0f));
								SkeletonJSON_new.ReadCurve(dictionary4, rotateTimeline, num6);
								num6++;
							}
							exposedList.Add(rotateTimeline);
							num = Math.Max(num, rotateTimeline.Frames[(rotateTimeline.FrameCount - 1) * 2]);
						}
						else
						{
							if (!(key4 == "translate") && !(key4 == "scale"))
							{
								bool flag = !(key4 == "shear");
							}
							if (!(key4 == "translate") && !(key4 == "scale") && !(key4 == "shear"))
							{
								throw new Exception(string.Concat(new string[]
								{
									"Invalid timeline type for a bone: ",
									key4,
									" (",
									key3,
									")"
								}));
							}
							float num7 = 1f;
							float defaultValue = 0f;
							key4 == "scale";
							TranslateTimeline translateTimeline;
							if (key4 == "scale")
							{
								translateTimeline = new ScaleTimeline(list2.Count);
								defaultValue = 1f;
							}
							else
							{
								key4 == "shear";
								if (key4 == "shear")
								{
									translateTimeline = new ShearTimeline(list2.Count);
								}
								else
								{
									translateTimeline = new TranslateTimeline(list2.Count);
									num7 = scale;
								}
							}
							translateTimeline.BoneIndex = num5;
							int num8 = 0;
							foreach (object obj5 in list2)
							{
								Dictionary<string, object> dictionary5 = (Dictionary<string, object>)obj5;
								float float4 = SkeletonJSON_new.GetFloat(dictionary5, "time", 0f);
								float float5 = SkeletonJSON_new.GetFloat(dictionary5, "x", defaultValue);
								float float6 = SkeletonJSON_new.GetFloat(dictionary5, "y", defaultValue);
								translateTimeline.SetFrame(num8, float4, float5 * num7, float6 * num7);
								SkeletonJSON_new.ReadCurve(dictionary5, translateTimeline, num8);
								num8++;
							}
							exposedList.Add(translateTimeline);
							num = Math.Max(num, translateTimeline.Frames[(translateTimeline.FrameCount - 1) * 3]);
						}
					}
				}
			}
			if (map.ContainsKey("ik"))
			{
				foreach (KeyValuePair<string, object> keyValuePair5 in ((Dictionary<string, object>)map["ik"]))
				{
					IkConstraintData ikConstraintData = skeletonData.FindIkConstraint(keyValuePair5.Key);
					List<object> list3 = (List<object>)keyValuePair5.Value;
					IkConstraintTimeline ikConstraintTimeline = new IkConstraintTimeline(list3.Count)
					{
						IkConstraintIndex = skeletonData.IkConstraints.IndexOf(ikConstraintData)
					};
					int num9 = 0;
					foreach (object obj6 in list3)
					{
						Dictionary<string, object> dictionary6 = (Dictionary<string, object>)obj6;
						ikConstraintTimeline.SetFrame(num9, SkeletonJSON_new.GetFloat(dictionary6, "time", 0f), SkeletonJSON_new.GetFloat(dictionary6, "mix", 1f), SkeletonJSON_new.GetFloat(dictionary6, "softness", 0f) * scale, SkeletonJSON_new.GetBoolean(dictionary6, "bendPositive", true) ? 1 : -1, SkeletonJSON_new.GetBoolean(dictionary6, "compress", false), SkeletonJSON_new.GetBoolean(dictionary6, "stretch", false));
						SkeletonJSON_new.ReadCurve(dictionary6, ikConstraintTimeline, num9);
						num9++;
					}
					exposedList.Add(ikConstraintTimeline);
					num = Math.Max(num, ikConstraintTimeline.Frames[(ikConstraintTimeline.FrameCount - 1) * 6]);
				}
			}
			if (map.ContainsKey("transform"))
			{
				foreach (KeyValuePair<string, object> keyValuePair6 in ((Dictionary<string, object>)map["transform"]))
				{
					TransformConstraintData transformConstraintData = skeletonData.FindTransformConstraint(keyValuePair6.Key);
					List<object> list4 = (List<object>)keyValuePair6.Value;
					TransformConstraintTimeline transformConstraintTimeline = new TransformConstraintTimeline(list4.Count)
					{
						TransformConstraintIndex = skeletonData.TransformConstraints.IndexOf(transformConstraintData)
					};
					int num10 = 0;
					foreach (object obj7 in list4)
					{
						Dictionary<string, object> dictionary7 = (Dictionary<string, object>)obj7;
						transformConstraintTimeline.SetFrame(num10, SkeletonJSON_new.GetFloat(dictionary7, "time", 0f), SkeletonJSON_new.GetFloat(dictionary7, "rotateMix", 1f), SkeletonJSON_new.GetFloat(dictionary7, "translateMix", 1f), SkeletonJSON_new.GetFloat(dictionary7, "scaleMix", 1f), SkeletonJSON_new.GetFloat(dictionary7, "shearMix", 1f));
						SkeletonJSON_new.ReadCurve(dictionary7, transformConstraintTimeline, num10);
						num10++;
					}
					exposedList.Add(transformConstraintTimeline);
					num = Math.Max(num, transformConstraintTimeline.Frames[(transformConstraintTimeline.FrameCount - 1) * 5]);
				}
			}
			if (map.ContainsKey("path"))
			{
				foreach (KeyValuePair<string, object> keyValuePair7 in ((Dictionary<string, object>)map["path"]))
				{
					int num11 = skeletonData.FindPathConstraintIndex(keyValuePair7.Key);
					if (num11 == -1)
					{
						throw new Exception("Path constraint not found: " + keyValuePair7.Key);
					}
					PathConstraintData pathConstraintData = skeletonData.PathConstraints.Items[num11];
					foreach (KeyValuePair<string, object> keyValuePair8 in ((Dictionary<string, object>)keyValuePair7.Value))
					{
						List<object> list5 = (List<object>)keyValuePair8.Value;
						string key5 = keyValuePair8.Key;
						if (!(key5 == "position"))
						{
							key5 == "spacing";
						}
						if (key5 == "position" || key5 == "spacing")
						{
							float num12 = 1f;
							key5 == "spacing";
							PathConstraintPositionTimeline pathConstraintPositionTimeline;
							if (key5 == "spacing")
							{
								pathConstraintPositionTimeline = new PathConstraintSpacingTimeline(list5.Count);
								if (pathConstraintData.SpacingMode != null)
								{
									bool flag2 = pathConstraintData.SpacingMode == 1;
								}
								if (pathConstraintData.SpacingMode == null || pathConstraintData.SpacingMode == 1)
								{
									num12 = scale;
								}
							}
							else
							{
								pathConstraintPositionTimeline = new PathConstraintPositionTimeline(list5.Count);
								PositionMode positionMode = pathConstraintData.PositionMode;
								if (pathConstraintData.PositionMode == null)
								{
									num12 = scale;
								}
							}
							pathConstraintPositionTimeline.PathConstraintIndex = num11;
							int num13 = 0;
							foreach (object obj8 in list5)
							{
								Dictionary<string, object> dictionary8 = (Dictionary<string, object>)obj8;
								pathConstraintPositionTimeline.SetFrame(num13, SkeletonJSON_new.GetFloat(dictionary8, "time", 0f), SkeletonJSON_new.GetFloat(dictionary8, key5, 0f) * num12);
								SkeletonJSON_new.ReadCurve(dictionary8, pathConstraintPositionTimeline, num13);
								num13++;
							}
							exposedList.Add(pathConstraintPositionTimeline);
							num = Math.Max(num, pathConstraintPositionTimeline.Frames[(pathConstraintPositionTimeline.FrameCount - 1) * 2]);
						}
						else
						{
							key5 == "mix";
							if (key5 == "mix")
							{
								PathConstraintMixTimeline pathConstraintMixTimeline = new PathConstraintMixTimeline(list5.Count)
								{
									PathConstraintIndex = num11
								};
								int num14 = 0;
								foreach (object obj9 in list5)
								{
									Dictionary<string, object> dictionary9 = (Dictionary<string, object>)obj9;
									pathConstraintMixTimeline.SetFrame(num14, SkeletonJSON_new.GetFloat(dictionary9, "time", 0f), SkeletonJSON_new.GetFloat(dictionary9, "rotateMix", 1f), SkeletonJSON_new.GetFloat(dictionary9, "translateMix", 1f));
									SkeletonJSON_new.ReadCurve(dictionary9, pathConstraintMixTimeline, num14);
									num14++;
								}
								exposedList.Add(pathConstraintMixTimeline);
								num = Math.Max(num, pathConstraintMixTimeline.Frames[(pathConstraintMixTimeline.FrameCount - 1) * 3]);
							}
						}
					}
				}
			}
			if (map.ContainsKey("deform"))
			{
				foreach (KeyValuePair<string, object> keyValuePair9 in ((Dictionary<string, object>)map["deform"]))
				{
					Skin skin = skeletonData.FindSkin(keyValuePair9.Key);
					foreach (KeyValuePair<string, object> keyValuePair10 in ((Dictionary<string, object>)keyValuePair9.Value))
					{
						int num15 = skeletonData.FindSlotIndex(keyValuePair10.Key);
						if (num15 == -1)
						{
							throw new Exception("Slot not found: " + keyValuePair10.Key);
						}
						foreach (KeyValuePair<string, object> keyValuePair11 in ((Dictionary<string, object>)keyValuePair10.Value))
						{
							List<object> list6 = (List<object>)keyValuePair11.Value;
							VertexAttachment vertexAttachment = (VertexAttachment)skin.GetAttachment(num15, keyValuePair11.Key);
							if (vertexAttachment == null)
							{
								throw new Exception("Deform attachment not found: " + keyValuePair11.Key);
							}
							bool flag3 = vertexAttachment.Bones != null;
							float[] vertices = vertexAttachment.Vertices;
							int num16 = flag3 ? (vertices.Length / 3 * 2) : vertices.Length;
							DeformTimeline deformTimeline = new DeformTimeline(list6.Count)
							{
								SlotIndex = num15,
								Attachment = vertexAttachment
							};
							int num17 = 0;
							foreach (object obj10 in list6)
							{
								Dictionary<string, object> dictionary10 = (Dictionary<string, object>)obj10;
								float[] array;
								if (!dictionary10.ContainsKey("vertices"))
								{
									array = (flag3 ? new float[num16] : vertices);
								}
								else
								{
									array = new float[num16];
									int @int = SkeletonJSON_new.GetInt(dictionary10, "offset", 0);
									float[] floatArray = SkeletonJSON_new.GetFloatArray(dictionary10, "vertices", 1f);
									Array.Copy(floatArray, 0, array, @int, floatArray.Length);
									if (scale != 1f)
									{
										int i = @int;
										int num18 = i + floatArray.Length;
										while (i < num18)
										{
											array[i] *= scale;
											i++;
										}
									}
									if (!flag3)
									{
										for (int j = 0; j < num16; j++)
										{
											array[j] += vertices[j];
										}
									}
								}
								deformTimeline.SetFrame(num17, SkeletonJSON_new.GetFloat(dictionary10, "time", 0f), array);
								SkeletonJSON_new.ReadCurve(dictionary10, deformTimeline, num17);
								num17++;
							}
							exposedList.Add(deformTimeline);
							num = Math.Max(num, deformTimeline.Frames[deformTimeline.FrameCount - 1]);
						}
					}
				}
			}
			if (map.ContainsKey("drawOrder") || map.ContainsKey("draworder"))
			{
				List<object> list7 = (List<object>)map[map.ContainsKey("drawOrder") ? "drawOrder" : "draworder"];
				DrawOrderTimeline drawOrderTimeline = new DrawOrderTimeline(list7.Count);
				int count = skeletonData.Slots.Count;
				int num19 = 0;
				foreach (object obj11 in list7)
				{
					Dictionary<string, object> dictionary11 = (Dictionary<string, object>)obj11;
					int[] array2 = null;
					dictionary11.ContainsKey("offsets");
					if (dictionary11.ContainsKey("offsets"))
					{
						array2 = new int[count];
						for (int k = count - 1; k >= 0; k--)
						{
							array2[k] = -1;
						}
						List<object> list8 = (List<object>)dictionary11["offsets"];
						int[] array3 = new int[count - list8.Count];
						int l = 0;
						int num20 = 0;
						foreach (object obj12 in list8)
						{
							Dictionary<string, object> dictionary12 = (Dictionary<string, object>)obj12;
							int num21 = skeletonData.FindSlotIndex((string)dictionary12["slot"]);
							if (num21 == -1)
							{
								string str = "Slot not found: ";
								object obj13 = dictionary12["slot"];
								throw new Exception(str + ((obj13 != null) ? obj13.ToString() : null));
							}
							while (l != num21)
							{
								array3[num20++] = l++;
							}
							int num22 = l + (int)((float)dictionary12["offset"]);
							array2[num22] = l++;
						}
						while (l < count)
						{
							array3[num20++] = l++;
						}
						for (int m = count - 1; m >= 0; m--)
						{
							int num23 = array2[m];
							if (array2[m] == -1)
							{
								array2[m] = array3[--num20];
							}
						}
					}
					drawOrderTimeline.SetFrame(num19++, SkeletonJSON_new.GetFloat(dictionary11, "time", 0f), array2);
				}
				exposedList.Add(drawOrderTimeline);
				num = Math.Max(num, drawOrderTimeline.Frames[drawOrderTimeline.FrameCount - 1]);
			}
			if (map.ContainsKey("events"))
			{
				List<object> list9 = (List<object>)map["events"];
				EventTimeline eventTimeline = new EventTimeline(list9.Count);
				int num24 = 0;
				foreach (object obj14 in list9)
				{
					Dictionary<string, object> dictionary13 = (Dictionary<string, object>)obj14;
					EventData eventData = skeletonData.FindEvent((string)dictionary13["name"]);
					if (eventData == null)
					{
						string str2 = "Event not found: ";
						object obj15 = dictionary13["name"];
						throw new Exception(str2 + ((obj15 != null) ? obj15.ToString() : null));
					}
					Event @event = new Event(SkeletonJSON_new.GetFloat(dictionary13, "time", 0f), eventData)
					{
						Int = SkeletonJSON_new.GetInt(dictionary13, "int", eventData.Int),
						Float = SkeletonJSON_new.GetFloat(dictionary13, "float", eventData.Float),
						String = SkeletonJSON_new.GetString(dictionary13, "string", eventData.String)
					};
					string audioPath = @event.Data.AudioPath;
					if (@event.Data.AudioPath != null)
					{
						@event.Volume = SkeletonJSON_new.GetFloat(dictionary13, "volume", eventData.Volume);
						@event.Balance = SkeletonJSON_new.GetFloat(dictionary13, "balance", eventData.Balance);
					}
					eventTimeline.SetFrame(num24++, @event);
				}
				exposedList.Add(eventTimeline);
				num = Math.Max(num, eventTimeline.Frames[eventTimeline.FrameCount - 1]);
			}
			exposedList.TrimExcess();
			skeletonData.Animations.Add(new Animation(name, exposedList, num));
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x000130B4 File Offset: 0x000112B4
		public static void ReadVertices(SkeletonJson __instance, Dictionary<string, object> map, VertexAttachment attachment, int verticesLength)
		{
			attachment.WorldVerticesLength = verticesLength;
			float[] floatArray = SkeletonJSON_new.GetFloatArray(map, "vertices", 1f);
			float scale = __instance.Scale;
			if (verticesLength == floatArray.Length)
			{
				if (scale != 1f)
				{
					for (int i = 0; i < floatArray.Length; i++)
					{
						floatArray[i] *= scale;
					}
				}
				attachment.Vertices = floatArray;
				return;
			}
			ExposedList<float> exposedList = new ExposedList<float>(verticesLength * 3 * 3);
			ExposedList<int> exposedList2 = new ExposedList<int>(verticesLength * 3);
			int j = 0;
			int num = floatArray.Length;
			while (j < num)
			{
				int num2 = (int)floatArray[j++];
				exposedList2.Add(num2);
				int num3 = j + num2 * 4;
				while (j < num3)
				{
					exposedList2.Add((int)floatArray[j]);
					exposedList.Add(floatArray[j + 1] * __instance.Scale);
					exposedList.Add(floatArray[j + 2] * __instance.Scale);
					exposedList.Add(floatArray[j + 3]);
					j += 4;
				}
			}
			attachment.Bones = exposedList2.ToArray();
			attachment.Vertices = exposedList.ToArray();
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x000131B8 File Offset: 0x000113B8
		public static Attachment ReadAttachment(SkeletonJson __instance, Dictionary<string, object> map, Skin skin, int slotIndex, string name, SkeletonData skeletonData)
		{
			float scale = __instance.Scale;
			AttachmentLoader attachmentLoader;
			try
			{
				attachmentLoader = (AttachmentLoader)__instance.GetType().GetField("attachmentLoader", AccessTools.all).GetValue(__instance);
			}
			catch (Exception ex)
			{
				File.WriteAllText(Application.dataPath + "/BaseMods/attachmenterror.log", ex.Message + Environment.NewLine + ex.StackTrace);
				return null;
			}
			name = SkeletonJSON_new.GetString(map, "name", name);
			string @string = SkeletonJSON_new.GetString(map, "type", "region");
			AttachmentType attachmentType = (AttachmentType)Enum.Parse(typeof(AttachmentType), @string, true);
			string string2 = SkeletonJSON_new.GetString(map, "path", name);
			Attachment result;
			switch (attachmentType)
			{
			case 0:
			{
				RegionAttachment regionAttachment = attachmentLoader.NewRegionAttachment(skin, name, string2);
				if (regionAttachment == null)
				{
					result = null;
				}
				else
				{
					regionAttachment.Path = string2;
					regionAttachment.X = SkeletonJSON_new.GetFloat(map, "x", 0f) * scale;
					regionAttachment.Y = SkeletonJSON_new.GetFloat(map, "y", 0f) * scale;
					regionAttachment.ScaleX = SkeletonJSON_new.GetFloat(map, "scaleX", 1f);
					regionAttachment.ScaleY = SkeletonJSON_new.GetFloat(map, "scaleY", 1f);
					regionAttachment.Rotation = SkeletonJSON_new.GetFloat(map, "rotation", 0f);
					regionAttachment.Width = SkeletonJSON_new.GetFloat(map, "width", 32f) * scale;
					regionAttachment.Height = SkeletonJSON_new.GetFloat(map, "height", 32f) * scale;
					if (map.ContainsKey("color"))
					{
						string hexString = (string)map["color"];
						regionAttachment.R = SkeletonJSON_new.ToColor(hexString, 0, 8);
						regionAttachment.G = SkeletonJSON_new.ToColor(hexString, 1, 8);
						regionAttachment.B = SkeletonJSON_new.ToColor(hexString, 2, 8);
						regionAttachment.A = SkeletonJSON_new.ToColor(hexString, 3, 8);
					}
					regionAttachment.UpdateOffset();
					result = regionAttachment;
				}
				break;
			}
			case 1:
			{
				BoundingBoxAttachment boundingBoxAttachment = attachmentLoader.NewBoundingBoxAttachment(skin, name);
				if (boundingBoxAttachment == null)
				{
					result = null;
				}
				else
				{
					SkeletonJSON_new.ReadVertices(__instance, map, boundingBoxAttachment, SkeletonJSON_new.GetInt(map, "vertexCount", 0) << 1);
					result = boundingBoxAttachment;
				}
				break;
			}
			case 2:
			case 3:
			{
				MeshAttachment meshAttachment = attachmentLoader.NewMeshAttachment(skin, name, string2);
				if (meshAttachment == null)
				{
					result = null;
				}
				else
				{
					meshAttachment.Path = string2;
					if (map.ContainsKey("color"))
					{
						string hexString2 = (string)map["color"];
						meshAttachment.R = SkeletonJSON_new.ToColor(hexString2, 0, 8);
						meshAttachment.G = SkeletonJSON_new.ToColor(hexString2, 1, 8);
						meshAttachment.B = SkeletonJSON_new.ToColor(hexString2, 2, 8);
						meshAttachment.A = SkeletonJSON_new.ToColor(hexString2, 3, 8);
					}
					meshAttachment.Width = SkeletonJSON_new.GetFloat(map, "width", 0f) * scale;
					meshAttachment.Height = SkeletonJSON_new.GetFloat(map, "height", 0f) * scale;
					string string3 = SkeletonJSON_new.GetString(map, "parent", null);
					if (string3 != null)
					{
						SkeletonJSON_new.linkedMeshes.Add(new SkeletonJSON_new.LinkedMesh(meshAttachment, SkeletonJSON_new.GetString(map, "skin", null), slotIndex, string3, SkeletonJSON_new.GetBoolean(map, "deform", true)));
						result = meshAttachment;
					}
					else
					{
						float[] floatArray = SkeletonJSON_new.GetFloatArray(map, "uvs", 1f);
						SkeletonJSON_new.ReadVertices(__instance, map, meshAttachment, floatArray.Length);
						meshAttachment.Triangles = SkeletonJSON_new.GetIntArray(map, "triangles");
						meshAttachment.RegionUVs = floatArray;
						meshAttachment.UpdateUVs();
						if (map.ContainsKey("hull"))
						{
							meshAttachment.HullLength = SkeletonJSON_new.GetInt(map, "hull", 0) * 2;
						}
						if (map.ContainsKey("edges"))
						{
							meshAttachment.Edges = SkeletonJSON_new.GetIntArray(map, "edges");
						}
						result = meshAttachment;
					}
				}
				break;
			}
			case 4:
			{
				PathAttachment pathAttachment = attachmentLoader.NewPathAttachment(skin, name);
				if (pathAttachment == null)
				{
					result = null;
				}
				else
				{
					pathAttachment.Closed = SkeletonJSON_new.GetBoolean(map, "closed", false);
					pathAttachment.ConstantSpeed = SkeletonJSON_new.GetBoolean(map, "constantSpeed", true);
					int @int = SkeletonJSON_new.GetInt(map, "vertexCount", 0);
					SkeletonJSON_new.ReadVertices(__instance, map, pathAttachment, @int << 1);
					pathAttachment.Lengths = SkeletonJSON_new.GetFloatArray(map, "lengths", scale);
					result = pathAttachment;
				}
				break;
			}
			case 5:
			{
				PointAttachment pointAttachment = attachmentLoader.NewPointAttachment(skin, name);
				if (pointAttachment == null)
				{
					result = null;
				}
				else
				{
					pointAttachment.X = SkeletonJSON_new.GetFloat(map, "x", 0f) * scale;
					pointAttachment.Y = SkeletonJSON_new.GetFloat(map, "y", 0f) * scale;
					pointAttachment.Rotation = SkeletonJSON_new.GetFloat(map, "rotation", 0f);
					result = pointAttachment;
				}
				break;
			}
			case 6:
			{
				ClippingAttachment clippingAttachment = attachmentLoader.NewClippingAttachment(skin, name);
				if (clippingAttachment == null)
				{
					result = null;
				}
				else
				{
					string string4 = SkeletonJSON_new.GetString(map, "end", null);
					if (string4 != null)
					{
						SlotData slotData = skeletonData.FindSlot(string4);
						ClippingAttachment clippingAttachment2 = clippingAttachment;
						SlotData slotData2 = slotData;
						if (slotData2 == null)
						{
							throw new Exception("Clipping end slot not found: " + string4);
						}
						clippingAttachment2.EndSlot = slotData2;
					}
					SkeletonJSON_new.ReadVertices(__instance, map, clippingAttachment, SkeletonJSON_new.GetInt(map, "vertexCount", 0) << 1);
					result = clippingAttachment;
				}
				break;
			}
			default:
				result = null;
				break;
			}
			return result;
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x000136E8 File Offset: 0x000118E8
		public static Attachment ReadAttachment_new(SkeletonJson __instance, Dictionary<string, object> map, Skin skin, int slotIndex, string name, SkeletonData skeletonData)
		{
			AttachmentLoader attachmentLoader = (AttachmentLoader)__instance.GetType().GetField("attachmentLoader", AccessTools.all).GetValue(__instance);
			float scale = __instance.Scale;
			name = SkeletonJSON_new.GetString(map, "name", name);
			string @string = SkeletonJSON_new.GetString(map, "type", "region");
			AttachmentType attachmentType = (AttachmentType)Enum.Parse(typeof(AttachmentType), @string, true);
			string string2 = SkeletonJSON_new.GetString(map, "path", name);
			Attachment result;
			switch (attachmentType)
			{
			case 0:
			{
				RegionAttachment regionAttachment = attachmentLoader.NewRegionAttachment(skin, name, string2);
				if (regionAttachment == null)
				{
					result = null;
				}
				else
				{
					regionAttachment.Path = string2;
					regionAttachment.X = SkeletonJSON_new.GetFloat(map, "x", 0f) * scale;
					regionAttachment.Y = SkeletonJSON_new.GetFloat(map, "y", 0f) * scale;
					regionAttachment.ScaleX = SkeletonJSON_new.GetFloat(map, "scaleX", 1f);
					regionAttachment.ScaleY = SkeletonJSON_new.GetFloat(map, "scaleY", 1f);
					regionAttachment.Rotation = SkeletonJSON_new.GetFloat(map, "rotation", 0f);
					regionAttachment.Width = SkeletonJSON_new.GetFloat(map, "width", 32f) * scale;
					regionAttachment.Height = SkeletonJSON_new.GetFloat(map, "height", 32f) * scale;
					if (map.ContainsKey("color"))
					{
						string hexString = (string)map["color"];
						regionAttachment.R = SkeletonJSON_new.ToColor(hexString, 0, 8);
						regionAttachment.G = SkeletonJSON_new.ToColor(hexString, 1, 8);
						regionAttachment.B = SkeletonJSON_new.ToColor(hexString, 2, 8);
						regionAttachment.A = SkeletonJSON_new.ToColor(hexString, 3, 8);
					}
					regionAttachment.UpdateOffset();
					result = regionAttachment;
				}
				break;
			}
			case 1:
			{
				BoundingBoxAttachment boundingBoxAttachment = attachmentLoader.NewBoundingBoxAttachment(skin, name);
				if (boundingBoxAttachment == null)
				{
					result = null;
				}
				else
				{
					SkeletonJSON_new.ReadVertices(__instance, map, boundingBoxAttachment, SkeletonJSON_new.GetInt(map, "vertexCount", 0) << 1);
					result = boundingBoxAttachment;
				}
				break;
			}
			case 2:
			case 3:
			{
				MeshAttachment meshAttachment = attachmentLoader.NewMeshAttachment(skin, name, string2);
				if (meshAttachment == null)
				{
					result = null;
				}
				else
				{
					meshAttachment.Path = string2;
					if (map.ContainsKey("color"))
					{
						string hexString2 = (string)map["color"];
						meshAttachment.R = SkeletonJSON_new.ToColor(hexString2, 0, 8);
						meshAttachment.G = SkeletonJSON_new.ToColor(hexString2, 1, 8);
						meshAttachment.B = SkeletonJSON_new.ToColor(hexString2, 2, 8);
						meshAttachment.A = SkeletonJSON_new.ToColor(hexString2, 3, 8);
					}
					meshAttachment.Width = SkeletonJSON_new.GetFloat(map, "width", 0f) * scale;
					meshAttachment.Height = SkeletonJSON_new.GetFloat(map, "height", 0f) * scale;
					string string3 = SkeletonJSON_new.GetString(map, "parent", null);
					if (string3 != null)
					{
						SkeletonJSON_new.linkedMeshes.Add(new SkeletonJSON_new.LinkedMesh(meshAttachment, SkeletonJSON_new.GetString(map, "skin", null), slotIndex, string3, true));
						result = meshAttachment;
					}
					else
					{
						float[] floatArray = SkeletonJSON_new.GetFloatArray(map, "uvs", 1f);
						SkeletonJSON_new.ReadVertices(__instance, map, meshAttachment, floatArray.Length);
						meshAttachment.Triangles = SkeletonJSON_new.GetIntArray(map, "triangles");
						meshAttachment.RegionUVs = floatArray;
						meshAttachment.UpdateUVs();
						if (map.ContainsKey("hull"))
						{
							meshAttachment.HullLength = SkeletonJSON_new.GetInt(map, "hull", 0) * 2;
						}
						if (map.ContainsKey("edges"))
						{
							meshAttachment.Edges = SkeletonJSON_new.GetIntArray(map, "edges");
						}
						result = meshAttachment;
					}
				}
				break;
			}
			case 4:
			{
				PathAttachment pathAttachment = attachmentLoader.NewPathAttachment(skin, name);
				if (pathAttachment == null)
				{
					result = null;
				}
				else
				{
					pathAttachment.Closed = SkeletonJSON_new.GetBoolean(map, "closed", false);
					pathAttachment.ConstantSpeed = SkeletonJSON_new.GetBoolean(map, "constantSpeed", true);
					int @int = SkeletonJSON_new.GetInt(map, "vertexCount", 0);
					SkeletonJSON_new.ReadVertices(__instance, map, pathAttachment, @int << 1);
					pathAttachment.Lengths = SkeletonJSON_new.GetFloatArray(map, "lengths", scale);
					result = pathAttachment;
				}
				break;
			}
			case 5:
			{
				PointAttachment pointAttachment = attachmentLoader.NewPointAttachment(skin, name);
				if (pointAttachment == null)
				{
					result = null;
				}
				else
				{
					pointAttachment.X = SkeletonJSON_new.GetFloat(map, "x", 0f) * scale;
					pointAttachment.Y = SkeletonJSON_new.GetFloat(map, "y", 0f) * scale;
					pointAttachment.Rotation = SkeletonJSON_new.GetFloat(map, "rotation", 0f);
					result = pointAttachment;
				}
				break;
			}
			case 6:
			{
				ClippingAttachment clippingAttachment = attachmentLoader.NewClippingAttachment(skin, name);
				if (clippingAttachment == null)
				{
					result = null;
				}
				else
				{
					string string4 = SkeletonJSON_new.GetString(map, "end", null);
					if (string4 != null)
					{
						SlotData slotData = skeletonData.FindSlot(string4);
						ClippingAttachment clippingAttachment2 = clippingAttachment;
						SlotData slotData2 = slotData;
						if (slotData2 == null)
						{
							throw new Exception("Clipping end slot not found: " + string4);
						}
						clippingAttachment2.EndSlot = slotData2;
					}
					SkeletonJSON_new.ReadVertices(__instance, map, clippingAttachment, SkeletonJSON_new.GetInt(map, "vertexCount", 0) << 1);
					result = clippingAttachment;
				}
				break;
			}
			default:
				result = null;
				break;
			}
			return result;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00013BC4 File Offset: 0x00011DC4
		public static float GetFloat(Dictionary<string, object> map, string name, float defaultValue)
		{
			float result;
			if (!map.ContainsKey(name))
			{
				result = defaultValue;
			}
			else
			{
				result = (float)map[name];
			}
			return result;
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x00013BEC File Offset: 0x00011DEC
		public static int GetInt(Dictionary<string, object> map, string name, int defaultValue)
		{
			int result;
			if (!map.ContainsKey(name))
			{
				result = defaultValue;
			}
			else
			{
				result = (int)((float)map[name]);
			}
			return result;
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x00013C18 File Offset: 0x00011E18
		public static bool GetBoolean(Dictionary<string, object> map, string name, bool defaultValue)
		{
			bool result;
			if (!map.ContainsKey(name))
			{
				result = defaultValue;
			}
			else
			{
				result = (bool)map[name];
			}
			return result;
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x00013C40 File Offset: 0x00011E40
		public static string GetString(Dictionary<string, object> map, string name, string defaultValue)
		{
			string result;
			if (!map.ContainsKey(name))
			{
				result = defaultValue;
			}
			else
			{
				result = (string)map[name];
			}
			return result;
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x00013C68 File Offset: 0x00011E68
		public static float[] GetFloatArray(Dictionary<string, object> map, string name, float scale)
		{
			List<object> list = (List<object>)map[name];
			float[] array = new float[list.Count];
			if (scale == 1f)
			{
				int i = 0;
				int count = list.Count;
				while (i < count)
				{
					array[i] = (float)list[i];
					i++;
				}
			}
			else
			{
				int j = 0;
				int count2 = list.Count;
				while (j < count2)
				{
					array[j] = (float)list[j] * scale;
					j++;
				}
			}
			return array;
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x00013CE8 File Offset: 0x00011EE8
		public static int[] GetIntArray(Dictionary<string, object> map, string name)
		{
			List<object> list = (List<object>)map[name];
			int[] array = new int[list.Count];
			int i = 0;
			int count = list.Count;
			while (i < count)
			{
				array[i] = (int)((float)list[i]);
				i++;
			}
			return array;
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00013D34 File Offset: 0x00011F34
		public static void ReadCurve(Dictionary<string, object> valueMap, CurveTimeline timeline, int frameIndex)
		{
			if (valueMap.ContainsKey("curve"))
			{
				object obj = valueMap["curve"];
				if (obj is string)
				{
					timeline.SetStepped(frameIndex);
					return;
				}
				timeline.SetCurve(frameIndex, (float)obj, SkeletonJSON_new.GetFloat(valueMap, "c2", 0f), SkeletonJSON_new.GetFloat(valueMap, "c3", 1f), SkeletonJSON_new.GetFloat(valueMap, "c4", 1f));
			}
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00013DA8 File Offset: 0x00011FA8
		public static float ToColor(string hexString, int colorIndex, int expectedLength = 8)
		{
			if (hexString.Length != expectedLength)
			{
				throw new ArgumentException(string.Concat(new object[]
				{
					"Color hexidecimal length must be ",
					expectedLength,
					", recieved: ",
					hexString
				}), "hexString");
			}
			return (float)Convert.ToInt32(hexString.Substring(colorIndex * 2, 2), 16) / 255f;
		}

		// Token: 0x04000125 RID: 293
		public static List<SkeletonJSON_new.LinkedMesh> linkedMeshes = new List<SkeletonJSON_new.LinkedMesh>();

		// Token: 0x02000086 RID: 134
		public class LinkedMesh
		{
			// Token: 0x06000364 RID: 868 RVA: 0x0001D008 File Offset: 0x0001B208
			public LinkedMesh(MeshAttachment mesh, string skin, int slotIndex, string parent, bool inheritDeform)
			{
				this.mesh = mesh;
				this.skin = skin;
				this.slotIndex = slotIndex;
				this.parent = parent;
				this.inheritDeform = inheritDeform;
			}

			// Token: 0x040001F5 RID: 501
			internal string parent;

			// Token: 0x040001F6 RID: 502
			internal string skin;

			// Token: 0x040001F7 RID: 503
			internal int slotIndex;

			// Token: 0x040001F8 RID: 504
			internal MeshAttachment mesh;

			// Token: 0x040001F9 RID: 505
			internal bool inheritDeform;
		}
	}
}
