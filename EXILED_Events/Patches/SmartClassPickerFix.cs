using GameCore;
using Harmony;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using Console = GameCore.Console;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EXILED.Patches
{
	[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.RunSmartClassPicker))]
	public class SmartClassPickerFix
	{
		public static bool Prefix(CharacterClassManager __instance)
		{
			try
			{
				Dictionary<ReferenceHub, RoleType> roles = new Dictionary<ReferenceHub, RoleType>();
				RoleType forcedClass = NonFacilityCompatibility.currentSceneSettings.forcedClass;
				ReferenceHub[] array = __instance.GetShuffledPlayerList().ToArray();
				RoundSummary.SumInfo_ClassList startClassList = default;
				bool flag = false;
				int num = 0;
				float[] array2 = new float[] { 0f, 0.4f, 0.6f, 0.5f };
				__instance._laterJoinNextIndex = 0;
				int r = EventPlugin.Gen.Next(1, 100);

				int i = 0;
				foreach(ReferenceHub hub in array)
				{
					if(hub.isDedicatedServer || !hub.isReady) continue;

					RoleType roleType = (RoleType)((__instance.ForceClass < RoleType.Scp173)
						? __instance.FindRandomIdUsingDefinedTeam(__instance.ClassTeamQueue[i++])
						: ((int)__instance.ForceClass));
					__instance._laterJoinNextIndex++;
					if (__instance.Classes.CheckBounds(forcedClass))
					{
						roleType = forcedClass;
					}

					if (r <= __instance.CiPercentage && roleType == RoleType.FacilityGuard)
						roleType = RoleType.ChaosInsurgency;

					switch (__instance.Classes.SafeGet(roleType).team)
					{
						case Team.SCP:
							startClassList.scps_except_zombies++;
							break;
						case Team.MTF:
							startClassList.mtf_and_guards++;
							break;
						case Team.CHI:
							startClassList.chaos_insurgents++;
							break;
						case Team.RSC:
							startClassList.scientists++;
							break;
						case Team.CDP:
							startClassList.class_ds++;
							break;
					}

					if (__instance.Classes.SafeGet(roleType).team == Team.SCP && !flag)
					{
						if (array2[Mathf.Clamp(num, 0, array2.Length)] > Random.value)
						{
							flag = true;
							__instance.Classes.Get(roleType).banClass = false;
							roleType = RoleType.Scp079;
						}

						num++;
					}


					if (!roles.ContainsKey(hub))
					{
						roles.Add(hub, roleType);
					}
					else
					{
						roles[hub] = roleType;
					}

					ServerLogs.AddLog(ServerLogs.Modules.ClassChange,
						string.Concat(hub.GetComponent<NicknameSync>().MyNick, " (",
							hub.GetComponent<CharacterClassManager>().UserId, ") spawned as ",
							__instance.Classes.SafeGet(roleType).fullName.Replace("\n", ""), "."),
						ServerLogs.ServerLogType.GameEvent);
				}

				startClassList.time = (int)Time.realtimeSinceStartup;
				startClassList.warhead_kills = -1;
				Object.FindObjectOfType<RoundSummary>().SetStartClassList(startClassList);
				if (ConfigFile.ServerConfig.GetBool("smart_class_picker", true))
				{
					string str = "Before Starting";
					try
					{
						str = "Setting Initial Value";
						if (ConfigFile.smBalancedPicker == null)
						{
							ConfigFile.smBalancedPicker = new Dictionary<string, int[]>();
						}

						str = "Valid Players List Error";
						List<ReferenceHub> shuffledPlayerList = __instance.GetShuffledPlayerList();
						str = "Copying Balanced Picker List";
						Dictionary<string, int[]> dictionary =
							new Dictionary<string, int[]>(ConfigFile.smBalancedPicker);
						str = "Clearing Balanced Picker List";
						ConfigFile.smBalancedPicker.Clear();
						str = "Re-building Balanced Picker List";
						foreach (ReferenceHub referenceHub in shuffledPlayerList)
						{
							if (referenceHub != null)
							{
								CharacterClassManager component = referenceHub.characterClassManager;
								NetworkConnection networkConnection = null;
								if (component != null)
								{
									networkConnection = (component.connectionToClient ?? component.connectionToServer);
								}

								str = "Getting Player ID";
								if (referenceHub.isDedicatedServer || !referenceHub.isReady || (networkConnection == null && component == null))
								{
									shuffledPlayerList.Remove(referenceHub);
									break;
								}

								if (__instance.SrvRoles.DoNotTrack)
								{
									shuffledPlayerList.Remove(referenceHub);
								}
								else
								{
									string str4 = (networkConnection != null) ? networkConnection.address : "";
									string str2 = (component != null) ? component.UserId : "";
									string text = str4 + str2;
									str = "Setting up Player \"" + text + "\"";
									if (!dictionary.ContainsKey(text))
									{
										str = "Adding Player \"" + text + "\" to smBalancedPicker";
										int[] arra = new int[__instance.Classes.Length];
										for (int j = 0; j < arra.Length; j++)
										{
											arra[j] = ConfigFile.ServerConfig.GetInt("smart_cp_starting_weight", 6);
										}

										ConfigFile.smBalancedPicker.Add(text, arra);
									}
									else
									{
										str = "Updating Player \"" + text + "\" in smBalancedPicker";

										if (dictionary.TryGetValue(text, out int[] value))
										{
											ConfigFile.smBalancedPicker.Add(text, value);
										}
									}
								}
							}
						}

						str = "Clearing Copied Balanced Picker List";
						dictionary.Clear();
						List<RoleType> list = new List<RoleType>();
						str = "Getting Available Roles";
						if (shuffledPlayerList.Contains(null))
						{
							shuffledPlayerList.Remove(null);
						}

						foreach (ReferenceHub referenceHub in shuffledPlayerList)
						{
							if (referenceHub != null)
							{
								RoleType rt = RoleType.None;
								roles.TryGetValue(referenceHub, out rt);
								if (rt != RoleType.None)
								{
									list.Add(rt);
								}
								else
								{
									shuffledPlayerList.Remove(referenceHub);
								}
							}
						}

						List<ReferenceHub> list2 = new List<ReferenceHub>();
						str = "Setting Roles";
						foreach (ReferenceHub referenceHub in shuffledPlayerList)
						{
							if (referenceHub != null)
							{
								CharacterClassManager component2 = referenceHub.GetComponent<CharacterClassManager>();
								NetworkConnection networkConnection2 = null;
								if (component2 != null)
								{
									networkConnection2 =
										(component2.connectionToClient ?? component2.connectionToServer);
								}

								if (networkConnection2 == null && component2 == null)
								{
									shuffledPlayerList.Remove(referenceHub);
									break;
								}

								string str5 = (networkConnection2 != null) ? networkConnection2.address : "";
								string str3 = (component2 != null) ? component2.UserId : "";
								string text2 = str5 + str3;
								str = "Setting Player \"" + text2 + "\"'s Class";
								RoleType mostLikelyClass = __instance.GetMostLikelyClass(text2, list);
								if (mostLikelyClass != RoleType.None)
								{
									if (!roles.ContainsKey(referenceHub))
									{
										roles.Add(referenceHub, mostLikelyClass);
									}
									else
									{
										roles[referenceHub] = mostLikelyClass;
									}

									ServerLogs.AddLog(ServerLogs.Modules.ClassChange,
										string.Concat(referenceHub.GetComponent<NicknameSync>().MyNick, " (",
											referenceHub.GetComponent<CharacterClassManager>().UserId, ") class set to ",
											__instance.Classes.SafeGet(mostLikelyClass).fullName.Replace("\n", ""),
											" by Smart Class Picker."), ServerLogs.ServerLogType.GameEvent);
									list.Remove(mostLikelyClass);
								}
								else
								{
									list2.Add(referenceHub);
								}
							}
						}

						str = "Reversing Additional Classes List";
						list.Reverse();
						str = "Setting Unknown Players Classes";
						foreach (ReferenceHub referenceHub in list2)
						{
							if (referenceHub == null)
								continue;
							if (list.Count > 0)
							{
								RoleType roleType2 = list[0];
								if (!roles.ContainsKey(referenceHub))
								{
									roles.Add(referenceHub, roleType2);
								}
								else
								{
									roles[referenceHub] = roleType2;
								}

								ServerLogs.AddLog(ServerLogs.Modules.ClassChange,
									string.Concat(referenceHub.GetComponent<NicknameSync>().MyNick, " (",
										referenceHub.GetComponent<CharacterClassManager>().UserId, ") class set to ",
										__instance.Classes.SafeGet(roleType2).fullName.Replace("\n", ""),
										" by Smart Class Picker."), ServerLogs.ServerLogType.GameEvent);
								list.Remove(roleType2);
							}
							else
							{
								roles.Add(referenceHub, RoleType.Spectator);
								ServerLogs.AddLog(ServerLogs.Modules.ClassChange,
									referenceHub.GetComponent<NicknameSync>().MyNick + " (" +
									referenceHub.GetComponent<CharacterClassManager>().UserId +
									") class set to SPECTATOR by Smart Class Picker.",
									ServerLogs.ServerLogType.GameEvent);
							}
						}

						str = "Clearing Unknown Players List";
						list2.Clear();
						str = "Clearing Available Classes List";
						list.Clear();
					}
					catch (Exception ex)
					{
						Console.AddLog("Smart Class Picker Failed: " + str + ", " + ex.Message,
							new Color32(byte.MaxValue, 180, 0, byte.MaxValue));
						return true;
					}
				}

				foreach (KeyValuePair<ReferenceHub, RoleType> rtr in roles)
				{
					__instance.SetPlayersClass(rtr.Value, rtr.Key.gameObject);
				}

				return false;
			}
			catch (Exception exception)
			{
				Log.Error($"SmartClassPickerFix error: {exception}");
				return true;
			}
		}
	}
}