using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Assets.Scripts.GameModes;
using Assets.Scripts.GSD.Models;
using Assets.Scripts.Helpers;
using Assets.Scripts.Models;
using ExitGames.Client.Photon;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class homeMenu : MonoBehaviour
{
	public void ToggleSound()
	{
		if (this.IsMenuSoundOn)
		{
			this.AdjustMenuSoundByValue(2);
			this.topPnl.globalSettings.Game_s.IsMenuSoundOn = 2;
		}
		else
		{
			this.AdjustMenuSoundByValue(1);
			this.topPnl.globalSettings.Game_s.IsMenuSoundOn = 1;
		}
		this.topPnl.SaveGameSettings();
	}

	public void AdjustMenuSoundByValue(int MenuSoundValue)
	{
		if (MenuSoundValue == 0 || MenuSoundValue == 1)
		{
			this.ImageMenuSound.sprite = this.SoundOn;
			this.MenuSound.enabled = true;
			this.IsMenuSoundOn = true;
			this.topPnl.globalSettings.Game_s.IsMenuSoundOn = 1;
		}
		else
		{
			this.ImageMenuSound.sprite = this.SoundOff;
			this.MenuSound.enabled = false;
			this.IsMenuSoundOn = false;
			this.topPnl.globalSettings.Game_s.IsMenuSoundOn = 2;
		}
	}

	private IEnumerator InitObjects()
	{
		if (!this.HasInitCompleted && !this.InitActionInProgress)
		{
			this.InitActionInProgress = true;
			string initLastStepCompleted = this.InitLastStepCompleted;
			switch (initLastStepCompleted)
			{
			{
				UnityEngine.Debug.Log("Init: menus");
				string text = "Rendering menus...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.LicenseActionPanel.transform.localPosition = new Vector2(0f, 0f);
				Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
				text = "Loading player settings...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.CheckForSettingsOverrides();
				this.topPnl.LoadSettingsFromDB();
				this.topPnl.LoadPlayerSetting();
				this.newRoundScript.PopulateRecipeDD();
				this.InitLastStepCompleted = "menus";
				this.InitActionInProgress = false;
				break;
			}
			case "menus":
			{
				UnityEngine.Debug.Log("Init: Check for Updates");
				string text = "Checking for updates...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.CheckForSystemUpdatea();
				break;
			}
			case "systemcheck":
			{
				this.SetLicenseValidState();
				string text = "Course init...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.PrepCourseObjectsForLoading();
				this.PrepareCourseDatabase();
				this.InitLastStepCompleted = "PrepCourseObjectsForLoading";
				this.InitActionInProgress = false;
				break;
			}
			case "PrepCourseObjectsForLoading":
			{
				string text = "Building course list...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.failCount = 0;
				this.ProcessCourseImages();
				break;
			}
			case "ProcessCourseImages":
			{
				string text = "Cleaning up courses...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.courseManaager.ClearDownloadingCourses();
				this.courseManaager.GetCourseRepos();
				this.BuildCourseManagerObjectsV2();
				this.InitLastStepCompleted = "BuildCourseObjects";
				this.InitActionInProgress = false;
				break;
			}
			case "BuildCourseObjects":
			{
				string text = "Loading data panels...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				UnityEngine.Debug.Log("Init: Set Panels");
				GameObject gosp = GameObject.Find("splash2");
				this.homeMenu_go.SetActive(false);
				this.homeMenu_go.SetActive(true);
				this.splash.enabled = false;
				if (gosp != null)
				{
					UnityEngine.Object.Destroy(gosp);
				}
				this.MainMenuInit();
				this.InitLastStepCompleted = "setpanels";
				this.InitActionInProgress = false;
				break;
			}
			case "setpanels":
			{
				string text = "Loading leaderboard...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				UnityEngine.Debug.Log("Init: Load Marquee");
				this.InitLastStepCompleted = "loadmarquee";
				this.InitActionInProgress = false;
				break;
			}
			case "loadmarquee":
			{
				UnityEngine.Debug.Log("Init: Final CleanUp");
				string text = "Final cleanup...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.mainGameControler.dataPnl.LoadDPSettings();
				this.InitLastStepCompleted = "finalcleanup";
				this.InitActionInProgress = false;
				break;
			}
			case "finalcleanup":
			{
				UnityEngine.Debug.Log("Init: Marked As Done");
				this.ImageStaticLoading.gameObject.SetActive(false);
				string text = "Finishing Up...";
				this.LoadingScreenMessage.text = text;
				yield return text;
				this.courseManaager.AddCourseToLocalDB("GSProPracticeFacility", true);
				this.courseManaager.AddCourseToLocalDB("GSPRange22", true);
				yield return new WaitForSeconds(3f);
				this.HasInitCompleted = true;
				this.InitActionInProgress = false;
				this.KillLoadingScreen();
				break;
			}
			}
		}
		yield break;
	}

	private void Start()
	{
		this.BaseTex = new Texture2D(2, 2);
	}

	public void KillLoadingScreen()
	{
		UnityEngine.Debug.Log("KillLoadingScreen");
		this.FadeInOutLib.GameObjectToFadeAway = this.LoadingFlagCanvasHolder.gameObject;
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.LoadingFlagCanvasHolder.gameObject;
		if (!this.mainGameControler.IsSimulatorControls() && !this.mainGameControler.MessageDialog.activeSelf)
		{
			this.DisplayCourseError("Simulator control disabled. Mouse Control Enabled. Go to Settings to Change.", 2);
		}
	}

	private IEnumerator CheckLicenseCoroutine()
	{
		yield return base.StartCoroutine(this.CheckLicenseCoroutineACtual());
		UnityEngine.Debug.Log("Done checking for License");
		yield break;
	}

	private IEnumerator CheckLicenseCoroutineACtual()
	{
		yield return null;
		this.CheckLicense();
		yield break;
	}

	public bool SystemCheckIsDone()
	{
		return this.BoolSystemCheckIsDone;
	}

	public bool AreCoursesLoaded()
	{
		return this.CoursesLoaded;
	}

	public void CheckLicnseAfterStartupSequence()
	{
		this.CheckLicense();
		if (this.IsLicenseValid)
		{
			this.mainGameControler.KillAllConnect();
			this.mainGameControler.StartConnect();
			this.FadeInOutLib.GameObjectToFadeAway = this.LicenseActionPanel.gameObject;
			this.FadeInOutLib.FadeToBlack();
			this.NavigationBackObject = this.LicenseActionPanel.gameObject;
		}
		else
		{
			this.mainGameControler.KillAllConnect();
		}
	}

	public void CheckLicense()
	{
		UnityEngine.Debug.Log("CheckLicense");
		string text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..";
		try
		{
			this.extL.SetFolderE(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
		}
		catch (Exception ex)
		{
			this.DisplayCourseError("Missing required C++ library. Click button to download and install.", 4);
		}
		if (!File.Exists(text + "\\gsp.lic"))
		{
			text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\..";
		}
		int productId = 12424;
		UnityEngine.Debug.Log("license path : " + text);
		if (LicenseLib.IsLicenseFileFound(text + "\\gsp.lic"))
		{
			string text2 = File.ReadAllText(text + "\\gsp.lic");
			this.LicenseKey = text2;
			List<string> list = new List<string>();
			list.Add("MPKAA-POFGU-AIMSW-VJKZQ");
			list.Add("IFDRO-NPPAN-QIQBZ-JLDFM");
			list.Add("CIOGX-YUIKM-TYYZJ-OYTOI");
			list.Add("IJPPC-DZLPI-WPQJF-SGWQB");
			list.Add("MNMCK-LSHER-CEAGU-KHHYI");
			list.Add("EOZKR-MJAZQ-KJLJN-VYUXX");
			list.Add("PFCT-BEKLC-GNHIU-VDHNQ");
			list.Add("KFOVC-MZVQY-WGMGM-ICYJN");
			list.Add("BVYGG-ZWJEL-ALXXN-JTZHY");
			list.Add("JMHOU-WGBDC-FWEDX-OFSXL");
			list.Add("DEFFI-UHHER-LJKIW-GXATE");
			list.Add("IUABE-PEHJT-BTFGZ-LPZHC");
			list.Add("JQCCC-RJTSS-IOYXC-DFXXC");
			list.Add("IDRLM-MHKBI-NMRAG-IQFIJ");
			list.Add("FPFCT-BEKLC-GNHIU-VDHNQ");
			list.Add("BUFZF-JIHEX-IUYFW-JDIOI");
			list.Add("DCDUB-ORVHN-ZUDHA-CHZLV");
			list.Add("HPLJM-XJFBJ-UKTDV-HSCCD");
			list.Add("LVHZB-XSGJE-TLXBS-XXJAX");
			list.Add("IUABE-PEHJT-BTFGZ-LPZHC");
			list.Add("MSFSS-WBTPD-EVMGY-UQLIM");
			list.Add("FJAAA-SJTPD-IXGUX-OUOVE");
			list.Add("FSRSN-TUDBK-DYOHM-GKHCC");
			if (this.IsUnstable && !list.Contains(text2))
			{
				UnityEngine.Debug.LogError("License not allowed for beta testing - " + text2);
			}
			if (this.extL.CheckLicenseDLL(productId, text2) && (list.Contains(text2) || !this.IsUnstable))
			{
				this.IsLicenseValid = true;
			}
			else
			{
				this.IsLicenseValid = false;
				this.LicenseActionMessage.text = "The current license associated is not valid." + Environment.NewLine + "Please purchase new license or contact support.";
			}
		}
		else
		{
			this.LicenseActionMessage.text = "No license found. Please enter your license information";
			this.IsLicenseValid = false;
		}
		this.BoolSystemCheckIsDone = true;
	}

	public void SetLicenseValidState()
	{
		if (this.IsLicenseValid)
		{
			UnityEngine.Debug.Log("License is Valid");
			LicenseLib.LicenseIsValid = true;
			this.LicenseActionPanel.SetActive(false);
			this.mainGameControler.KillAllConnect();
			this.mainGameControler.StartConnect();
		}
		else
		{
			UnityEngine.Debug.LogWarning("License is Invalid");
			LicenseLib.LicenseIsValid = false;
			this.LicenseActionPanel.SetActive(true);
			this.mainGameControler.KillAllConnect();
		}
	}

	public void CheckForSettingsOverrides()
	{
		try
		{
			string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\..\\settings-override.json";
			if (File.Exists(path))
			{
				string value = File.ReadAllText(path);
				this.mainGameControler.ExternalOverridesObj = JsonConvert.DeserializeObject<ExternalOverrides>(value);
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("Failed loading external settings");
			this.mainGameControler.ExternalOverridesObj = null;
		}
	}

	public void DownloadLatestStable()
	{
		this.UpdateDatFiles("FORCE", "Please restart GSPro and restart using the GSPro LAUNCHER");
	}

	public void DownloadLatestPublicBeta()
	{
		this.UpdateDatFiles("BETA", "Please restart GSPro and restart using the GSPro LAUNCHER - WARNING: Build unstable. Please report issues to bugs@gsprogolf.com");
	}

	public void UpdateDatFiles(string DatValue, string DialogMessage)
	{
		string str = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\..";
		try
		{
			if (File.Exists(str + "\\GSProV1.dat") && File.Exists(str + "\\GSProV1Connect.dat"))
			{
				File.WriteAllText(str + "\\GSProV1.dat", DatValue);
				File.WriteAllText(str + "\\GSProV1Connect.dat", DatValue);
				this.DisplayCourseError("UPDATE READY" + Environment.NewLine + Environment.NewLine + DialogMessage, 2);
			}
			else
			{
				this.DisplayCourseError("An error occured - like due to Windows folder permissions or antivirus software", 2);
			}
		}
		catch (Exception ex)
		{
			this.DisplayCourseError("An error occured - like due to Windows folder permissions or antivirus software", 2);
		}
	}

	public void CheckForSystemUpdatea()
	{
		if (SystemUpdate.CheckForUpdate())
		{
			this.DisplayCourseError("Updating GSPro system files. Please wait. DO NOT close GSPro!", 0);
			if (SystemUpdate.DownloadUpdate())
			{
				this.DisplayCourseError("Your core GSPro fies were automatically updated. Click button to close GSPro and then restart", 3);
			}
			else
			{
				this.DisplayCourseError("Core update failed. We will try again on next start.", 2);
			}
		}
		SystemUpdate.GetCourseElevationOverrides();
		this.BlackListedCourses = SystemUpdate.GetCourseBlackList();
		this.CheckLicense();
		this.InitLastStepCompleted = "systemcheck";
		this.InitActionInProgress = false;
	}

	public void HandleSystemCheckResult(bool WasUpdateFound)
	{
		UnityEngine.Debug.Log(string.Format("HandleSystemCheckResult, WasUpdateFound: {0}", WasUpdateFound));
		UnityEngine.Debug.Log("Checking For License Coroutine");
		base.StartCoroutine(this.CheckLicenseCoroutine());
	}

	private void ImageProcessGenericError(string ErrorMessage)
	{
		UnityEngine.Debug.LogWarning("Coure Image Load failed");
		this.failCount++;
		this.fileLoadFailed = false;
		UnityEngine.Debug.LogWarning("Error Loading Course: " + this.courseName + " | " + ErrorMessage);
		if (File.Exists(this.dirs[this.courseCount] + "/image_altered_1920_1080_" + this.splashName))
		{
			UnityEngine.Debug.LogWarning("Deleting Rendered Image");
			File.Delete(this.dirs[this.courseCount] + "/image_altered_1920_1080_" + this.splashName);
		}
	}

	public System.Drawing.Image ResizeImageKeepAspectRatio(System.Drawing.Image source, int width, int height)
	{
		System.Drawing.Image result = null;
		try
		{
			if (source.Width != width || source.Height != height)
			{
				float num = (float)source.Width / (float)source.Height;
				using (Bitmap bitmap = new Bitmap(width, height))
				{
					using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
					{
						graphics.CompositingQuality = CompositingQuality.HighQuality;
						graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
						graphics.SmoothingMode = SmoothingMode.HighQuality;
						float num2 = (float)source.Height / (float)height;
						float num3 = (float)source.Width / (float)width;
						float num4;
						if (num3 < num2)
						{
							num4 = num3;
						}
						else
						{
							num4 = num2;
						}
						int num5 = (int)((float)source.Width / num4);
						int num6 = (int)((float)source.Height / num4);
						if (num5 < width)
						{
							num5 = width;
						}
						if (num6 < height)
						{
							num6 = height;
						}
						int num7 = 0;
						int num8 = 0;
						if (num5 > width)
						{
							num7 = (num5 - width) / 2;
						}
						if (num6 > height)
						{
							num8 = (num6 - height) / 2;
						}
						graphics.DrawImage(source, -num7, -num8, num5, num6);
					}
					result = new Bitmap(bitmap);
				}
			}
			else
			{
				result = new Bitmap(source);
			}
		}
		catch (Exception)
		{
			result = null;
		}
		return result;
	}

	protected bool PrepareCourseImageForTexture(string FilePath, string SavePath)
	{
		System.Drawing.Image source = System.Drawing.Image.FromFile(FilePath);
		System.Drawing.Image image = this.ResizeImageKeepAspectRatio(source, 480, 270);
		image.Save(SavePath, ImageFormat.Jpeg);
		image.Dispose();
		return true;
	}

	public void LoadImage(string dirName, string thisSplashName, string thisCourseName, GolfCourse passedGolfCourse)
	{
		thisCourseName = thisCourseName.ToLower();
		bool flag = true;
		bool flag2 = true;
		byte[] data = new byte[0];
		string text = string.Empty;
		string text2 = string.Empty;
		try
		{
			if (thisSplashName.Equals(string.Empty))
			{
				data = this.DefaultCourseImage.texture.EncodeToPNG();
			}
			else if (File.Exists(dirName + "/image_altered_480_270" + thisSplashName.Split(new char[]
			{
				'.'
			}).FirstOrDefault<string>() + ".jpg"))
			{
				text2 = dirName + "\\image_altered_480_270" + thisSplashName.Split(new char[]
				{
					'.'
				}).FirstOrDefault<string>() + ".jpg";
				data = File.ReadAllBytes(text2);
				text = "\\image_altered_480_270" + thisSplashName.Split(new char[]
				{
					'.'
				}).FirstOrDefault<string>() + ".jpg";
			}
			else
			{
				text2 = dirName + "/" + thisSplashName;
				if (this.PrepareCourseImageForTexture(text2, dirName + "\\image_altered_480_270" + thisSplashName.Split(new char[]
				{
					'.'
				}).FirstOrDefault<string>() + ".jpg"))
				{
					text2 = dirName + "\\image_altered_480_270" + thisSplashName.Split(new char[]
					{
						'.'
					}).FirstOrDefault<string>() + ".jpg";
					data = File.ReadAllBytes(text2);
					text = "\\image_altered_480_270" + thisSplashName.Split(new char[]
					{
						'.'
					}).FirstOrDefault<string>() + ".jpg";
				}
				else
				{
					data = File.ReadAllBytes(text2);
				}
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogWarning("Image Error Start");
			UnityEngine.Debug.LogWarning(ex.Message);
			UnityEngine.Debug.LogWarning(ex.StackTrace);
			UnityEngine.Debug.LogWarning("Image Error End");
			flag = false;
			this.ImageProcessGenericError(ex.Message);
		}
		if (flag)
		{
			if (!this.SplashTextureDictionary.ContainsKey(thisCourseName))
			{
				Texture2D texture2D = new Texture2D(2, 2);
				bool flag3 = texture2D.LoadImage(data);
				try
				{
					Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, 480f, 270f), new Vector2(0.5f, 0.5f));
					this.SplashTextureDictionary.Add(thisCourseName, value);
				}
				catch (Exception ex2)
				{
					UnityEngine.Debug.LogWarning(ex2.Message);
					UnityEngine.Debug.LogWarning(ex2.StackTrace);
					flag2 = false;
					this.ImageProcessGenericError(ex2.Message + "|||" + ex2.StackTrace);
				}
			}
			if (flag2)
			{
				this.SplashTextures.Add(this.SplashTextureDictionary[thisCourseName]);
				this.namesOfCourses.Add(thisCourseName);
				if (passedGolfCourse != null)
				{
					if (!this.ActualNamesOfCourses.Contains(passedGolfCourse.CourseName))
					{
						this.ActualNamesOfCourses.Add(passedGolfCourse.CourseName);
					}
					if (!this.CourseNameDictionary.ContainsKey(thisCourseName))
					{
						this.CourseNameDictionary.Add(thisCourseName, passedGolfCourse.CourseName.ToLower());
					}
				}
				else
				{
					UnityEngine.Debug.Log("NOT Addnig course name");
				}
			}
		}
		this.fileLoadFailed = false;
		this.courseCount++;
		this.CheckIfCoursesAreDoneLoading();
	}

	public void CheckIfCoursesAreDoneLoading()
	{
		if (this.courseCount + this.failCount == this.dirs.Length)
		{
			UnityEngine.Debug.Log("All Images Have Been Processed");
			UnityEngine.Debug.Log(string.Format("OG Course Count: {0} OG Fail Count: {1}", this.courseCount, this.failCount));
			this.InitLastStepCompleted = "ProcessCourseImages";
			this.InitActionInProgress = false;
			this.CourseInitLastStepCompleted = "ProcessCourseImages";
			this.CourseInitInProgress = false;
		}
	}

	public void LoadCoursesInBackground()
	{
	}

	private Texture2D duplicateTexture(Texture2D source)
	{
		byte[] rawTextureData = source.GetRawTextureData();
		Texture2D texture2D = new Texture2D(source.width, source.height, source.format, false);
		texture2D.LoadRawTextureData(rawTextureData);
		texture2D.Apply();
		return texture2D;
	}

	public void ProcessCourseImages()
	{
		this.courseCount = 0;
		if (this.dirs.Length == 0)
		{
			UnityEngine.Debug.Log("All Images Have Been Processed");
			UnityEngine.Debug.Log(string.Format("OG Course Count: {0} OG Fail Count: {1}", this.courseCount, this.failCount));
			this.InitLastStepCompleted = "ProcessCourseImages";
			this.InitActionInProgress = false;
			this.CourseInitLastStepCompleted = "ProcessCourseImages";
			this.CourseInitInProgress = false;
		}
		else
		{
			foreach (string text in this.dirs)
			{
				this.fileLoadFailed = false;
				string[] source = text.Split(new char[]
				{
					'\\'
				});
				this.courseName = source.Last<string>();
				this.splashName = string.Empty;
				try
				{
					if (File.Exists(text + "/" + this.courseName + ".gkd") && File.Exists(text + "/" + this.courseName + ".unity3d") && !File.Exists(text + "/" + this.courseName + ".description"))
					{
						this.thisGolfCourse = JsonConvert.DeserializeObject<GolfCourse>(File.ReadAllText(text + "/" + this.courseName + ".gkd"));
						this.splashName = this.thisGolfCourse.CourseImageFileName;
						if (!File.Exists(text + "/" + this.splashName))
						{
							this.fileLoadFailed = true;
							this.failCount++;
							this.CheckIfCoursesAreDoneLoading();
						}
					}
					else
					{
						this.fileLoadFailed = true;
						this.failCount++;
						this.CheckIfCoursesAreDoneLoading();
					}
				}
				catch (Exception ex)
				{
					this.failCount++;
					this.fileLoadFailed = true;
					this.CheckIfCoursesAreDoneLoading();
				}
				if (!this.fileLoadFailed)
				{
					this.LoadImage(text.Clone().ToString(), this.splashName.Clone().ToString(), this.courseName.Clone().ToString(), this.thisGolfCourse);
				}
			}
		}
	}

	public void PrepCourseObjectsForLoading()
	{
		this.SplashTextures.Clear();
		this.namesOfCourses.Clear();
		this.ActualNamesOfCourses.Clear();
		bool flag = false;
		if (this.topPnl.globalSettings.Game_s.CourseFolder != string.Empty)
		{
			if (Directory.Exists(this.topPnl.globalSettings.Game_s.CourseFolder))
			{
				this.dirs = Directory.GetDirectories(this.topPnl.globalSettings.Game_s.CourseFolder);
				flag = true;
				this.selectedPath = this.topPnl.globalSettings.Game_s.CourseFolder;
			}
			else
			{
				UnityEngine.Debug.LogWarning("The folder from settings " + this.topPnl.globalSettings.Game_s.CourseFolder + " does not exist/wrong format");
			}
		}
		string path = Application.dataPath + "/../Courses";
		if (Directory.Exists(path) && !flag)
		{
			this.dirs = Directory.GetDirectories(path);
			flag = true;
			this.selectedPath = path;
		}
		if (Directory.Exists(this.pathS) && !flag)
		{
			this.dirs = Directory.GetDirectories(this.pathS);
			flag = true;
			this.selectedPath = this.pathS;
		}
		if (Directory.Exists(this.pathSa) && !flag)
		{
			this.dirs = Directory.GetDirectories(this.pathSa);
			flag = true;
			this.selectedPath = this.pathSa;
		}
		if (!flag)
		{
			string text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..";
			UnityEngine.Debug.Log(text);
			try
			{
				Directory.CreateDirectory(text + "\\Courses");
				this.dirs = Directory.GetDirectories(text + "\\Courses");
				this.selectedPath = text + "\\Courses";
				UnityEngine.Debug.Log("Courses Directory Created");
			}
			catch (Exception ex)
			{
				string text2 = "Could not find Courses in default location. \n";
				text2 += "1.Fill in the course folder setting \n";
				text2 += "2.Press \"Save/Close\"\n3.Restart the game. \n";
				text2 += " The format is for ex. C:\\GSProV1\\core\\GSP\\Courses ";
				this.stngCanvas.enabled = true;
				this.msgBoxS.MsgBoxPrint(true, false, false, "OK", string.Empty, string.Empty, text2, 80);
				this.msgBoxS.gameObject.SetActive(true);
				return;
			}
		}
		this.CourseFolderPath = this.selectedPath;
		UnityEngine.Debug.Log("Course Folder " + this.CourseFolderPath);
		this.courseCount = 0;
	}

	public void ToggleRoundHistory(bool show)
	{
		if (show)
		{
			this.RoundHistoryMainPnl.SetActive(true);
		}
		else
		{
			this.RoundHistoryMainPnl.SetActive(false);
		}
	}

	public void GetCourseRoundHistory(string courseFolder, string FriendlyName)
	{
		this.RoundHistoryMainPnl.SetActive(true);
		RoundHistory component = this.RoundHistoryMainPnl.GetComponent<RoundHistory>();
		component.PopulateRoundHistory(FriendlyName, courseFolder);
	}

	public void PrepareCourseDatabase()
	{
		CourseRepoCourse courseRepoCourse = (from x in this.db.ds.GetCourses()
		where x.CourseKey.Equals("TEMP")
		select x).FirstOrDefault<CourseRepoCourse>();
		if (courseRepoCourse != null)
		{
			this.db.ds.DeleteCourse(courseRepoCourse.ID);
		}
		this.db.ds.InsertCourse(new CourseRepoCourse
		{
			CourseKey = "TEMP",
			Name = "TEMP",
			DownloadURL = string.Empty,
			RemoteVersion = string.Empty,
			LocalVersion = string.Empty,
			Status = 2,
			CourseLocation = string.Empty,
			CourseDesigner = string.Empty,
			DownloadURL2 = string.Empty,
			DownloadURL3 = string.Empty,
			GKDDownloadURL2 = string.Empty,
			GKDDownloadURL3 = string.Empty
		});
		CourseRepoCourse courseRepoCourse2 = (from x in this.db.ds.GetCourses()
		where x.CourseKey.Equals("TEMP")
		select x).FirstOrDefault<CourseRepoCourse>();
		if (courseRepoCourse2 != null)
		{
			this.db.ds.DeleteCourse(courseRepoCourse2.ID);
		}
		List<CourseRepoCourse> list = this.db.ds.GetCourses().ToList<CourseRepoCourse>();
		string str = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\..\\..\\..\\..";
		string path = str + "\\temp\\course_manifest.dat";
		List<CourseRepoCourse> source = new List<CourseRepoCourse>();
		List<string> list2 = new List<string>();
		list2.Add("CourseLighting".ToLower());
		if (File.Exists(path))
		{
			source = JsonConvert.DeserializeObject<List<CourseRepoCourse>>(File.ReadAllText(path));
		}
		this.courseManaager.LoadCoursesFromRepos();
		List<CourseRepoCourse> getAllCourses = this.courseManaager.GetAllCourses;
		if (getAllCourses != null)
		{
			source = getAllCourses;
		}
		using (List<CourseRepoCourse>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				CourseRepoCourse thisDBCourse = enumerator.Current;
				bool flag = false;
				CourseRepoCourse courseRepoCourse3 = (from x in source
				where x.CourseKey != null && x.CourseKey.ToLower().Equals(thisDBCourse.CourseKey.ToLower())
				select x).FirstOrDefault<CourseRepoCourse>();
				if (courseRepoCourse3 != null)
				{
					if (thisDBCourse.LocalVersion != courseRepoCourse3.RemoteVersion)
					{
						thisDBCourse.Status = 8;
						thisDBCourse.Keywords = courseRepoCourse3.Keywords;
						thisDBCourse.KeywordsString = courseRepoCourse3.KeywordsString;
						thisDBCourse.DownloadURL = courseRepoCourse3.DownloadURL;
						thisDBCourse.LocalVersion = courseRepoCourse3.RemoteVersion;
						thisDBCourse.GKDDownloadURL = courseRepoCourse3.GKDDownloadURL;
						thisDBCourse.GKDVersion = courseRepoCourse3.GKDVersion;
						thisDBCourse.LastUpdated = courseRepoCourse3.LastUpdated;
						thisDBCourse.DownloadURL2 = ((thisDBCourse.DownloadURL2 == null) ? null : thisDBCourse.DownloadURL2);
						thisDBCourse.DownloadURL3 = ((thisDBCourse.DownloadURL3 == null) ? null : thisDBCourse.DownloadURL3);
						thisDBCourse.GKDDownloadURL2 = ((thisDBCourse.GKDDownloadURL2 == null) ? null : thisDBCourse.GKDDownloadURL2);
						thisDBCourse.GKDDownloadURL3 = ((thisDBCourse.GKDDownloadURL3 == null) ? null : thisDBCourse.GKDDownloadURL3);
						this.db.ds.UpdateCourse(thisDBCourse);
					}
					else if (courseRepoCourse3.GKDDownloadURL != null && courseRepoCourse3.GKDDownloadURL != string.Empty && courseRepoCourse3.GKDVersion != null && courseRepoCourse3.GKDVersion != string.Empty && courseRepoCourse3.GKDVersion != "-1" && thisDBCourse.GKDVersion != courseRepoCourse3.GKDVersion)
					{
						thisDBCourse.Status = 9;
						thisDBCourse.Keywords = courseRepoCourse3.Keywords;
						thisDBCourse.KeywordsString = courseRepoCourse3.KeywordsString;
						thisDBCourse.DownloadURL = courseRepoCourse3.DownloadURL;
						thisDBCourse.LocalVersion = courseRepoCourse3.RemoteVersion;
						thisDBCourse.GKDDownloadURL = courseRepoCourse3.GKDDownloadURL;
						thisDBCourse.GKDVersion = courseRepoCourse3.GKDVersion;
						thisDBCourse.LastUpdated = courseRepoCourse3.LastUpdated;
						thisDBCourse.DownloadURL2 = ((thisDBCourse.DownloadURL2 == null) ? null : thisDBCourse.DownloadURL2);
						thisDBCourse.DownloadURL3 = ((thisDBCourse.DownloadURL3 == null) ? null : thisDBCourse.DownloadURL3);
						thisDBCourse.GKDDownloadURL2 = ((thisDBCourse.GKDDownloadURL2 == null) ? null : thisDBCourse.GKDDownloadURL2);
						thisDBCourse.GKDDownloadURL3 = ((thisDBCourse.GKDDownloadURL3 == null) ? null : thisDBCourse.GKDDownloadURL3);
						this.db.ds.UpdateCourse(thisDBCourse);
					}
				}
				if (thisDBCourse.CourseFolder == null || thisDBCourse.CourseFolder == string.Empty)
				{
					if (courseRepoCourse3 != null)
					{
						thisDBCourse.CourseFolder = courseRepoCourse3.CourseFolder;
						this.db.ds.UpdateCourse(thisDBCourse);
					}
					else if (!list2.Contains(thisDBCourse.CourseKey.ToLower()))
					{
						flag = true;
						UnityEngine.Debug.LogWarning("Delete DB course due to not in SGT meta data: " + thisDBCourse.CourseKey.ToLower());
					}
				}
				if (thisDBCourse.CourseFolder == null || thisDBCourse.CourseFolder == string.Empty)
				{
					flag = true;
					UnityEngine.Debug.LogWarning("Killing course as meta data still did not hvae course folder");
				}
				if (!list2.Contains(thisDBCourse.CourseKey.ToLower()) && !flag && (this.dirs.Length == 0 || (from x in this.dirs
				where x.ToLower().Contains(thisDBCourse.CourseFolder.ToLower())
				select x).FirstOrDefault<string>() == null))
				{
					flag = true;
					UnityEngine.Debug.LogWarning("Delete DB course due to not physically available: " + thisDBCourse.CourseKey.ToLower());
				}
				if (!flag && this.dirs.Length > 0 && (from x in this.dirs
				where x.ToLower().Contains(thisDBCourse.CourseFolder.ToLower())
				select x).FirstOrDefault<string>() != null)
				{
					string str2 = (from x in this.dirs
					where x.ToLower().Contains(thisDBCourse.CourseFolder.ToLower())
					select x).FirstOrDefault<string>();
					if (!File.Exists(str2 + "\\" + thisDBCourse.CourseFolder.ToLower() + ".unity3d") || !File.Exists(str2 + "\\" + thisDBCourse.CourseFolder.ToLower() + ".gkd"))
					{
						flag = true;
						UnityEngine.Debug.LogWarning("Mising Unity3d or GKD file: " + thisDBCourse.CourseKey.ToLower());
					}
				}
				else if (!list2.Contains(thisDBCourse.CourseKey.ToLower()))
				{
					flag = true;
					UnityEngine.Debug.LogWarning("Delete DB course due to not physically available");
				}
				if (flag)
				{
					this.db.ds.DeleteCourse(thisDBCourse.ID);
				}
			}
		}
		if (this.dirs.Length == 1 && this.dirs[0] == string.Empty)
		{
			this.dirs = new string[0];
		}
		if (this.dirs.Length > 0)
		{
			list = this.db.ds.GetCourses().ToList<CourseRepoCourse>();
			string[] array = this.dirs;
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				string courseName = text.Remove(0, this.selectedPath.Length + 1);
				if (File.Exists(text + "\\" + courseName + ".unity3d") && File.Exists(text + "\\" + courseName + ".GKD"))
				{
					CourseRepoCourse courseRepoCourse4 = (from x in list
					where x.CourseFolder != null && x.CourseFolder.ToLower().Equals(courseName.ToLower())
					select x).FirstOrDefault<CourseRepoCourse>();
					if (courseRepoCourse4 != null)
					{
						try
						{
							if ((courseRepoCourse4.CourseDesigner == null || courseRepoCourse4.CourseDesigner == string.Empty || courseRepoCourse4.CourseLocation == null || courseRepoCourse4.CourseLocation == string.Empty || courseRepoCourse4.LastUpdated.ToShortDateString().Contains("1/1/0001")) && (from x in source
							where x.CourseFolder != null && x.CourseFolder.ToLower().Equals(courseName.ToLower())
							select x).FirstOrDefault<CourseRepoCourse>() != null)
							{
								CourseRepoCourse courseRepoCourse5 = (from x in source
								where x.CourseFolder != null && x.CourseFolder.ToLower().Equals(courseName.ToLower())
								select x).FirstOrDefault<CourseRepoCourse>();
								courseRepoCourse4.CourseLocation = courseRepoCourse5.CourseLocation;
								courseRepoCourse4.CourseDesigner = courseRepoCourse5.CourseDesigner;
								courseRepoCourse4.LastUpdated = courseRepoCourse5.LastUpdated;
								this.db.ds.UpdateCourse(courseRepoCourse4);
							}
						}
						catch (Exception ex)
						{
							string empty = string.Empty;
						}
					}
					else if ((from x in source
					where x.CourseFolder != null && x.CourseFolder.ToLower().Equals(courseName.ToLower())
					select x).FirstOrDefault<CourseRepoCourse>() != null)
					{
						CourseRepoCourse courseRepoCourse6 = (from x in source
						where x.CourseFolder.ToLower().Equals(courseName.ToLower())
						select x).FirstOrDefault<CourseRepoCourse>();
						this.db.ds.InsertCourse(new CourseRepoCourse
						{
							CourseKey = courseRepoCourse6.CourseKey,
							Name = courseRepoCourse6.Name,
							DownloadURL = courseRepoCourse6.DownloadURL,
							RemoteVersion = courseRepoCourse6.RemoteVersion,
							GKDVersion = courseRepoCourse6.GKDVersion,
							LocalVersion = courseRepoCourse6.RemoteVersion,
							Status = 2,
							CourseFolder = courseName.ToLower(),
							CourseLocation = courseRepoCourse6.CourseLocation,
							CourseDesigner = courseRepoCourse6.CourseDesigner,
							LastUpdated = courseRepoCourse6.LastUpdated,
							DownloadURL2 = ((courseRepoCourse6.DownloadURL2 == null) ? null : courseRepoCourse6.DownloadURL2),
							DownloadURL3 = ((courseRepoCourse6.DownloadURL3 == null) ? null : courseRepoCourse6.DownloadURL3),
							GKDDownloadURL2 = ((courseRepoCourse6.GKDDownloadURL2 == null) ? null : courseRepoCourse6.GKDDownloadURL2),
							GKDDownloadURL3 = ((courseRepoCourse6.GKDDownloadURL3 == null) ? null : courseRepoCourse6.GKDDownloadURL3)
						});
					}
					else if (File.Exists(text + "\\" + courseName + ".gkd"))
					{
						GolfCourse golfCourse = JsonConvert.DeserializeObject<GolfCourse>(File.ReadAllText(text + "\\" + courseName + ".gkd"));
						this.db.ds.InsertCourse(new CourseRepoCourse
						{
							CourseKey = golfCourse.CourseName,
							Name = golfCourse.CourseName,
							DownloadURL = string.Empty,
							RemoteVersion = string.Empty,
							LocalVersion = string.Empty,
							Status = 2,
							CourseFolder = courseName.ToLower(),
							CourseLocation = string.Empty,
							CourseDesigner = golfCourse.Designer,
							DownloadURL2 = string.Empty,
							DownloadURL3 = string.Empty,
							GKDDownloadURL2 = string.Empty,
							GKDDownloadURL3 = string.Empty
						});
					}
					else
					{
						this.db.ds.InsertCourse(new CourseRepoCourse
						{
							CourseKey = courseName,
							Name = courseName,
							DownloadURL = string.Empty,
							RemoteVersion = string.Empty,
							LocalVersion = string.Empty,
							Status = 2,
							CourseLocation = string.Empty,
							CourseDesigner = string.Empty,
							DownloadURL2 = string.Empty,
							DownloadURL3 = string.Empty,
							GKDDownloadURL2 = string.Empty,
							GKDDownloadURL3 = string.Empty
						});
					}
				}
			}
		}
	}

	public int CheckIfCourseIsPlayable(string CourseFolder)
	{
		if (File.Exists(string.Concat(new string[]
		{
			this.CourseFolderPath,
			"/",
			CourseFolder,
			"/",
			CourseFolder,
			".unity3d"
		})) && File.Exists(string.Concat(new string[]
		{
			this.CourseFolderPath,
			"/",
			CourseFolder,
			"/",
			CourseFolder,
			".gkd"
		})))
		{
			return 1;
		}
		return 0;
	}

	public void BuildCourseManagerObjectsV2()
	{
		this.ListCourseDataObject = (from x in this.db.ds.GetCourses()
		where x.CourseFolder != string.Empty
		orderby x.Name
		select x).ToList<CourseRepoCourse>();
		foreach (CourseRepoCourse courseRepoCourse in this.ListCourseDataObject)
		{
			courseRepoCourse.Keywords += courseRepoCourse.KeywordsString;
			courseRepoCourse.IsPlayable = this.CheckIfCourseIsPlayable(courseRepoCourse.CourseFolder);
		}
		List<CourseRepoCourse> coursesListFromAllRepos = this.courseManaager.GetCoursesListFromAllRepos();
		if (coursesListFromAllRepos != null && coursesListFromAllRepos.Count > 0)
		{
			using (IEnumerator<CourseRepoCourse> enumerator2 = (from x in coursesListFromAllRepos
			where x.CourseKey != null
			select x).GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					CourseRepoCourse thisRepoCourse = enumerator2.Current;
					if ((from x in this.ListCourseDataObject
					where x.CourseKey != null && x.CourseKey.ToLower().Equals(thisRepoCourse.CourseKey.ToLower())
					select x).FirstOrDefault<CourseRepoCourse>() == null)
					{
						thisRepoCourse.Status = 5;
						thisRepoCourse.IsPlayable = 0;
						thisRepoCourse.Keywords += thisRepoCourse.KeywordsString;
						this.ListCourseDataObject.Add(thisRepoCourse);
					}
				}
			}
		}
		this.ListCourseDataObject.RemoveAll((CourseRepoCourse x) => x.CourseKey.Equals("CourseLighting"));
		this.ListCourseDataObject.RemoveAll((CourseRepoCourse x) => x.CourseKey.Equals("GSPRange22"));
		if (this.BlackListedCourses != null && this.BlackListedCourses.Count > 0)
		{
			using (List<string>.Enumerator enumerator3 = this.BlackListedCourses.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					string thisCourse = enumerator3.Current;
					this.ListCourseDataObject.RemoveAll((CourseRepoCourse x) => x.CourseKey.Equals(thisCourse));
				}
			}
		}
		this.thisCourseScroller._data = this.ListCourseDataObject.ToList<CourseRepoCourse>();
		this.thisCourseScroller.ExternalReload();
		this.namesOfCourses.Clear();
		int num = 0;
		foreach (CourseRepoCourse courseRepoCourse2 in this.ListCourseDataObject)
		{
			if (File.Exists(string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				courseRepoCourse2.CourseFolder,
				"/",
				courseRepoCourse2.CourseFolder,
				".unity3d"
			})) && File.Exists(string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				courseRepoCourse2.CourseFolder,
				"/",
				courseRepoCourse2.CourseFolder,
				".gkd"
			})))
			{
				this.namesOfCourses.Add(courseRepoCourse2.CourseFolder);
				num++;
			}
		}
		if (this.thisCourseScroller._data.Count == 0)
		{
			this.ToggleCourseShowOnline.isOn = true;
		}
		this.courseImageVisibleSlot0 = 0u;
		this.BtnReloadCourses.GetComponentInChildren<Text>().text = "Reload Courses";
		this.InitLastStepCompleted = "loadcourses";
		this.InitActionInProgress = false;
	}

	public void CourseListingSwitchView(int ViewStyle)
	{
		if (ViewStyle == 1)
		{
			this.CourseListingUseAltView = false;
			this.BtnCourseListViewImage.color = new Color32(26, 128, 200, byte.MaxValue);
			this.BtnCourseGridViewImage.color = new Color32(112, 112, 112, byte.MaxValue);
			this.thisCourseScroller.thisScroller.gameObject.GetComponent<ScrollRect>().scrollSensitivity = 50f;
		}
		else
		{
			this.CourseListingUseAltView = true;
			this.BtnCourseGridViewImage.color = new Color32(26, 128, 200, byte.MaxValue);
			this.BtnCourseListViewImage.color = new Color32(112, 112, 112, byte.MaxValue);
			this.thisCourseScroller.thisScroller.gameObject.GetComponent<ScrollRect>().scrollSensitivity = 100f;
		}
		this.thisCourseScroller.ExternalReload();
	}

	public void SearchCourses(string thisText)
	{
		this.thisCourseScroller._data = (from x in this.ListCourseDataObject
		where (x.Name != null && x.Name.ToLower().Contains(thisText.ToLower())) || (x.CourseLocation != null && x.CourseLocation.ToLower().Contains(thisText.ToLower())) || (x.Keywords != null && x.Keywords.ToLower().Contains(thisText.ToLower())) || (x.CourseDesigner != null && x.CourseDesigner.ToLower().Contains(thisText.ToLower()))
		select x).ToList<CourseRepoCourse>();
		this.SortCourseListings(this.CourseSortIndex);
		this.thisCourseScroller.ExternalReload();
	}

	public void ToggleCourseListingShowOnline(bool isOn)
	{
		this.thisCourseScroller._data = this.ListCourseDataObject.ToList<CourseRepoCourse>();
		if (this.CourseSearchInput.text != string.Empty)
		{
			this.SearchCourses(this.CourseSearchInput.text);
		}
		this.SortCourseListings(this.CourseSortIndex);
		this.thisCourseScroller.ExternalReload();
	}

	public void SortCourseListings(int SelectedIndex)
	{
		this.CourseSortIndex = SelectedIndex;
		if (SelectedIndex == 0)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.Name
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 1)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.Name descending
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 2)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.LastUpdated descending
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 3)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.LastUpdated
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 4)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.CourseLocation
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 5)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.CourseLocation descending
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 6)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.CourseDesigner
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 7)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.CourseDesigner descending
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 8)
		{
			int[] LogicalOrder = new int[]
			{
				2,
				8,
				9,
				6,
				3,
				4,
				5,
				0,
				1
			};
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby Array.IndexOf<int>(LogicalOrder, x.Status)
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 9)
		{
			int[] LogicalOrder = new int[]
			{
				2,
				8,
				9,
				6,
				3,
				4,
				5,
				0,
				1
			};
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby Array.IndexOf<int>(LogicalOrder, x.Status) descending
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
		else if (SelectedIndex == 10)
		{
			this.thisCourseScroller._data = (from x in this.thisCourseScroller._data
			orderby x.IsFavoriteCourse descending
			select x).ToList<CourseRepoCourse>();
			this.thisCourseScroller.ExternalReload();
		}
	}

	public void BuildCourseObjects()
	{
		UnityEngine.Debug.Log("Starting BuildCourseObjects");
		IEnumerator enumerator = this.CourseSelectHolder.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		this.namesOfCourses.Clear();
		int num = 0;
		using (Dictionary<string, string>.Enumerator enumerator2 = this.CourseNameDictionary.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				KeyValuePair<string, string> ThisCourseItem = enumerator2.Current;
				homeMenu _0024this = this;
				this.namesOfCourses.Add(ThisCourseItem.Key);
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.CourseSelectSinglePreFab, this.CourseSelectHolder.transform);
				gameObject.GetComponentInChildren<SuperTextMesh>().text = ThisCourseItem.Value;
				UnityEngine.UI.Image[] componentsInChildren = gameObject.GetComponentsInChildren<UnityEngine.UI.Image>();
				foreach (UnityEngine.UI.Image image in componentsInChildren)
				{
					if (image.name.Contains("courseImage"))
					{
						image.sprite = this.SplashTextureDictionary[ThisCourseItem.Key];
						image.type = UnityEngine.UI.Image.Type.Filled;
						Button componentInChildren = image.GetComponentInChildren<Button>();
						int num2 = num.CloneJson<int>();
						componentInChildren.onClick.AddListener(delegate()
						{
							_0024this.img_click_index(ThisCourseItem.Key, ThisCourseItem.Value);
						});
					}
				}
				num++;
			}
		}
		this.courseImageVisibleSlot0 = 0u;
		this.BtnReloadCourses.GetComponentInChildren<Text>().text = "Reload Courses";
		this.InitLastStepCompleted = "loadcourses";
		this.InitActionInProgress = false;
	}

	public string WithMaxLength(string value, int maxLength)
	{
		if (value == null)
		{
			return null;
		}
		return value.Substring(0, Math.Min(value.Length, maxLength));
	}

	private void Update()
	{
		if (!this.HasInitCompleted && !this.InitActionInProgress)
		{
			UnityEngine.Debug.Log("Starting Main Init");
			base.StartCoroutine(this.InitObjects());
		}
		else if (!this.HasCoursesInitCompleted && !this.CourseInitInProgress && this.mainGameControler.ActiveGameState == 0)
		{
			UnityEngine.Debug.Log("Starting Course Init");
			this.menuOrchestrator.ReloadCourses();
		}
	}

	public void OnCoursePracticeClick()
	{
		this.newRoundScript.CourseImage.gameObject.SetActive(true);
		this.newRoundScript.CvsOnlinePlayChat.gameObject.SetActive(false);
		this.newRoundScript.PnlPlayersInTheRoom.gameObject.SetActive(false);
		this.newRoundScript.TxtOnlineLabel.text = "PLAYERS IN ROOM";
		this.newRoundScript.InvitePlayers.SetActive(false);
		this.newRoundScript.ResetForNewRound();
		this.newRndPP.ResetForNewRound();
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.practiceMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.selectCourseMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.practiceMenu_go;
		this.ActiveMenuObject = this.selectCourseMenu_go;
		for (int i = 0; i < 8; i++)
		{
			this.mainGameControler.player_a[i].color = i;
		}
		this.mainGameControler.ActiveGameType = 254;
		this.newRoundScript.ToggleMatchSettingsTab(2);
	}

	public void LocalGameClick()
	{
		this.mainGameControler.ResetCriticalRoundVaraibles();
		this.FullCoursePathNoExtension = string.Empty;
		this.mainGameControler.ActiveGameIsTournament = false;
		this.mainGameControler.ActiveGameIsOnline = false;
		this.newRoundScript.CourseImage.gameObject.SetActive(true);
		this.newRoundScript.CvsOnlinePlayChat.gameObject.SetActive(false);
		this.newRoundScript.TxtOnlineLabel.text = "PLAYERS IN ROOM";
		this.newRoundScript.PnlPlayersInTheRoom.gameObject.SetActive(false);
		this.mainGameControler.ActiveGameIsTournament = false;
		this.newRoundScript.InvitePlayers.SetActive(true);
		this.mainGameControler.ActiveGameType = 0;
		for (int i = 0; i < 8; i++)
		{
			this.mainGameControler.player_a[i].color = i;
		}
		this.newRoundScript.ResetForNewRound();
		this.newRndPP.ResetForNewRound();
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.homeMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.selectCourseMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.homeMenu_go;
		this.ActiveMenuObject = this.selectCourseMenu_go;
		this.GameObjectPriorToSettings = this.homeMenu_go;
		this.newRoundScript.ToggleMatchSettingsTab(1);
	}

	public void TournamentsClick()
	{
		this.mainGameControler.ResetCriticalRoundVaraibles();
		this.SGTMessageDialog.SetActive(false);
		this.FullCoursePathNoExtension = string.Empty;
		this.mainGameControler.ActiveGameIsOnline = false;
		if (this.mainGameControler.ActiveGameTournamentIsResume || this.tourMnu.StartBtnText.text.ToUpper().Equals("RESUME"))
		{
			this.newRoundScript.InvitePlayers.SetActive(false);
		}
		else
		{
			this.newRoundScript.InvitePlayers.SetActive(true);
		}
		this.newRoundScript.CourseImage.gameObject.SetActive(true);
		this.newRoundScript.TxtOnlineLabel.text = "PLAYERS IN ROOM";
		this.newRoundScript.CvsOnlinePlayChat.gameObject.SetActive(false);
		this.newRoundScript.PnlPlayersInTheRoom.gameObject.SetActive(false);
		this.mainGameControler.ActiveGameType = 0;
		for (int i = 0; i < 8; i++)
		{
			this.mainGameControler.player_a[i].color = i;
		}
		this.newRoundScript.ResetForNewRound();
		this.newRndPP.ResetForNewRound();
		this.tourMnu.startDelay = 40;
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.homeMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.TournamentMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.homeMenu_go;
		this.ActiveMenuObject = this.TournamentMenu_go;
		this.GameObjectPriorToSettings = this.homeMenu_go;
		this.newRoundScript.ToggleMatchSettingsTab(1);
	}

	public void OnlinePlayBackClick()
	{
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.OnlinePlayMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.OnlinePlayMenu_go;
		this.ActiveMenuObject = this.homeMenu_go;
	}

	public void OnlinePlayClick()
	{
		this.mainGameControler.ResetCriticalRoundVaraibles();
		if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom(true);
		}
		this.mainGameControler.ActiveGameIsOnline = true;
		this.newRoundScript.InvitePlayers.SetActive(false);
		this.mainGameControler.ActiveGameType = 0;
		for (int i = 0; i < 8; i++)
		{
			this.mainGameControler.player_a[i].color = i;
		}
		this.newRoundScript.ResetForNewRound();
		this.newRndPP.ResetForNewRound();
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.homeMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.OnlinePlayMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.homeMenu_go;
		this.ActiveMenuObject = this.OnlinePlayMenu_go;
		this.GameObjectPriorToSettings = this.homeMenu_go;
		this.newRoundScript.ToggleMatchSettingsTab(2);
	}

	public void InvitePlayersClick()
	{
		this.mainGameControler.ActiveGameIsOnline = true;
		this.newRoundScript.InvitePlayers.SetActive(false);
		this.mainGameControler.ActiveGameType = 0;
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.newRoundMenu;
		this.FadeInOutLib.GameObjectToFadeIn = this.OnlinePlayMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.newRoundMenu;
		this.ActiveMenuObject = this.OnlinePlayMenu_go;
		this.GameObjectPriorToSettings = this.homeMenu_go;
	}

	public void TournamentBackClick()
	{
		this.mainGameControler.EndRoundCleanUp();
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.TournamentMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.TournamentMenu_go;
		this.ActiveMenuObject = this.homeMenu_go;
	}

	public void PracticeClick()
	{
		this.mainGameControler.ResetCriticalRoundVaraibles();
		this.FullCoursePathNoExtension = string.Empty;
		this.mainGameControler.ActiveGameIsOnline = false;
		this.mainGameControler.ActiveGameIsTournament = false;
		for (int i = 0; i < 18; i++)
		{
			this.mainGameControler.ActiveGameHoles[i].active = true;
			this.mainGameControler.parseDescFile.newRoundScript.HoleSelect[i].isOn = true;
			this.mainGameControler.parseDescFile.newRoundScript.HoleSelect[i].gameObject.SetActive(true);
		}
		this.mainGameControler.gameMode = new GameMode(new StrokePlay(this.mainGameControler));
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.homeMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.practiceMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.homeMenu_go;
		this.ActiveMenuObject = this.practiceMenu_go;
		this.GameObjectPriorToSettings = this.homeMenu_go;
		this.mainGameControler.ActiveGameType = 255;
	}

	public void practiceBackClick()
	{
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.practiceMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		this.FadeInOutLib.FadeToBlack();
		this.GameObjectPriorToSettings = this.homeMenu_go;
	}

	public void selectCourseBackClick()
	{
		if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.LeaveRoom(true);
		}
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.selectCourseMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		this.FadeInOutLib.FadeToBlack();
	}

	public void HomeClick()
	{
		this.homeMenu_go.SetActive(true);
		this.selectCourseMenu_go.SetActive(false);
	}

	public void RoundEndInit()
	{
		this.HomeMenuBackground.enabled = true;
		GameObject gameObject = GameObject.Find("SubPlayer1");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer1");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer2");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer3");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer4");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer5");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer6");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("SubPlayer7");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		gameObject = GameObject.Find("TopPlayerBase");
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
		this.MainMenuMoveIn();
		this.assetBundle.Unload(true);
		GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
		foreach (GameObject gameObject2 in array)
		{
			string name = gameObject2.name;
			if (gameObject2.activeInHierarchy)
			{
				if (name.IndexOf("Terrain", 0) < 0)
				{
					if (name.IndexOf("terrain", 0) < 0)
					{
						goto IL_18B;
					}
				}
				try
				{
					UnityEngine.Object.Destroy(gameObject2);
				}
				catch (Exception ex)
				{
				}
			}
			IL_18B:;
		}
	}

	private string GetSplashFileName(string path)
	{
		StreamReader streamReader = new StreamReader(path);
		this.sCD = streamReader.ReadToEnd();
		streamReader.Close();
		return this.GetTag1("splashName", '"', 13, 0);
	}

	private void ParseCoursefile(string path)
	{
	}

	private string GetTag1(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sCD.IndexOf(tag, startoffs);
		int num2 = this.sCD.IndexOf(delimiter, num + offset);
		return this.sCD.Substring(num + offset, num2 - num - offset);
	}

	private string GetTag2(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sTD.IndexOf(tag, startoffs);
		int num2 = this.sTD.IndexOf(delimiter, num + offset);
		return this.sTD.Substring(num + offset, num2 - num - offset);
	}

	private string GetTag3(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sTD.IndexOf(tag, startoffs);
		int num2 = this.sTD.IndexOf(delimiter, num + offset);
		int num3 = this.sTD.IndexOf(',', num + offset);
		int num4 = this.sTD.IndexOf('}', num + offset);
		if (num3 < 0)
		{
			num3 = num4 + 1;
		}
		if (num4 < num3)
		{
			return this.sTD.Substring(num2 + 1, num4 - num2 - 1);
		}
		return this.sTD.Substring(num2 + 1, num3 - num2 - 1);
	}

	private string GetTag4(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sPD.IndexOf(tag, startoffs);
		int num2 = this.sPD.IndexOf(delimiter, num + offset);
		return this.sPD.Substring(num + offset, num2 - num - offset);
	}

	private string GetTag5(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sPD.IndexOf(tag, startoffs);
		int num2 = this.sPD.IndexOf(delimiter, num + offset);
		int num3 = this.sPD.IndexOf(',', num + offset);
		int num4 = this.sPD.IndexOf('}', num + offset);
		if (num4 < num3 || num3 == -1)
		{
			return this.sPD.Substring(num2 + 1, num4 - num2 - 1);
		}
		return this.sPD.Substring(num2 + 1, num3 - num2 - 1);
	}

	private string GetTag6(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sSD.IndexOf(tag, startoffs);
		int num2 = this.sSD.IndexOf(delimiter, num + offset);
		return this.sSD.Substring(num + offset, num2 - num - offset);
	}

	private string GetTag7(string tag, char delimiter, int offset, int startoffs)
	{
		int num = this.sSD.IndexOf(tag, startoffs);
		int num2 = this.sSD.IndexOf(delimiter, num + offset);
		int num3 = this.sSD.IndexOf(',', num + offset);
		int num4 = this.sSD.IndexOf('}', num + offset);
		if (num3 < 0)
		{
			num3 = num4 + 1;
		}
		if (num4 < num3)
		{
			return this.sSD.Substring(num2 + 1, num4 - num2 - 1);
		}
		return this.sSD.Substring(num2 + 1, num3 - num2 - 1);
	}

	private IEnumerator LoadScene(string sceneName)
	{
		yield return new WaitForSeconds(2f);
		this.ProgressTxt.gameObject.SetActive(true);
		AsyncOperation asyncOperation = Application.LoadLevelAsync(sceneName);
		this.mainGameControler.LoadedCourse = sceneName;
		asyncOperation.allowSceneActivation = false;
		while (!asyncOperation.isDone)
		{
			this.ProgressTxt.text = "Loading " + (asyncOperation.progress * 100f).ToString("F0") + "%";
			this.CourseLoadingProgressText.text = "Loading " + (asyncOperation.progress * 100f).ToString("F0") + "%";
			this.loadProgress = (int)asyncOperation.progress * 100;
			if (asyncOperation.progress >= 0.9f)
			{
				this.ProgressTxt.text = "Course Load " + (asyncOperation.progress * 100f).ToString("F0") + "%";
				this.CourseLoadingProgressText.text = "Course Load " + (asyncOperation.progress * 100f).ToString("F0") + "%";
			}
			asyncOperation.allowSceneActivation = true;
			yield return null;
		}
		this.ProgressTxt.text = "Processing Course ";
		this.CourseLoadingProgressText.text = "Processing Course ";
		this.ProgressTxt.gameObject.SetActive(false);
		this.courseLoadingScreen.SetActive(false);
		yield break;
	}

	public void PrepTournament(int courseNameIdx, int tournamentIdx, string courseSelected)
	{
		string text = string.Empty;
		if (Directory.Exists(this.CourseFolderPath))
		{
			text = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				courseSelected,
				"/",
				courseSelected,
				".gkd"
			});
			this.FullCoursePathNoExtension = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				courseSelected,
				"/",
				courseSelected
			});
			this.CourseName = courseSelected;
		}
		else if (this.FullCoursePathNoExtension != string.Empty)
		{
			text = this.FullCoursePathNoExtension + ".gkd";
		}
		else
		{
			UnityEngine.Debug.LogWarning("The path to the course folder is wrong(" + this.CourseFolderPath + ")");
		}
		if (File.Exists(text))
		{
			UnityEngine.Debug.LogWarning("Loading Course file " + text);
			this.parseDesc.ParseGKData(text);
			this.ActiveGKDPath = text;
		}
		this.populateTeeSelectionDD();
		this.SelectedCourseImage.sprite = this.SplashTextures.ElementAt(courseNameIdx);
		this.newRoundMenu.SetActive(true);
		this.newRndPP.updatePanels();
		this.newRoundScript.SetPracticeMode();
		this.newRoundScript.EnableDDs();
		this.newRoundScript.ResumeRound.isOn = false;
		this.mainGameControler.PerviousPlayableRoundFound = false;
		this.newRoundScript.PanelResumeRound.SetActive(false);
		this.newRoundScript.ToggleResumeRoundReact(false);
		this.newRoundScript.SetupGameVariablesTournament(tournamentIdx);
		this.newRndPP.LoadGameVariables();
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.TournamentMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.newRoundMenu;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.newRoundScript.ToggleMatchSettingsTab(2);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.TournamentMenu_go;
		this.ActiveMenuObject = this.newRoundMenu;
	}

	public void LoadCourseTournament(int courseNameIdx, int tournamentIdx)
	{
		this.courseLoadingScreen.transform.localPosition = new Vector2(0f, 0f);
		this.courseLoadingScreen.SetActive(true);
		this.newRoundMenu.SetActive(false);
		this.MenuGlobalWrapper.SetActive(false);
		string text;
		string dirPath;
		if (this.mainGameControler.ActiveGameIsOnline && !PhotonNetwork.IsMasterClient)
		{
			text = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				".unity3d"
			});
			dirPath = this.CourseFolderPath + "/" + PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString();
			string text2 = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				".gkd"
			});
			this.splash.GetComponent<UnityEngine.UI.Image>().sprite = (Sprite)Resources.Load("Sprites/Splash", typeof(Sprite));
			this.CourseLoadingImage.sprite = (Sprite)Resources.Load("Sprites/Splash", typeof(Sprite));
			this.CourseLoadingCoureName.text = "Tournament Round";
		}
		else if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsMasterClient)
		{
			text = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				".unity3d"
			});
			dirPath = this.CourseFolderPath + "/" + PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString();
			string text2 = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				"/",
				PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
				".gkd"
			});
			this.splash.sprite = this.SelectedCourseImage.sprite;
			this.CourseLoadingImage.sprite = this.SelectedCourseImage.sprite;
			this.CourseLoadingCoureName.text = "Tournament Round";
		}
		else
		{
			text = string.Concat(new string[]
			{
				this.CourseFolderPath,
				"/",
				this.CourseName,
				"/",
				this.CourseName,
				".unity3d"
			});
			dirPath = this.CourseFolderPath + "/" + this.CourseName;
			this.splash.sprite = this.SelectedCourseImage.sprite;
			this.CourseLoadingImage.sprite = this.SelectedCourseImage.sprite;
			this.CourseLoadingCoureName.text = "Tournament Round";
			if (File.Exists(text))
			{
				UnityEngine.Debug.Log("Loading Course file " + text);
			}
			else
			{
				UnityEngine.Debug.LogWarning("Loading file " + text + " failed");
			}
		}
		FileInfo fileInfo = new FileInfo(text);
		if (this.mainGameControler.RoundIsNew)
		{
			Round thisRound = new Round
			{
				PlayerName = this.topPnl.globalSettings.PlayerData_a[this.newRoundScript.newRoundPP.PlayerDDa[0].value - 1].Name,
				PlayerID = this.topPnl.globalSettings.PlayerData_a[this.newRoundScript.newRoundPP.PlayerDDa[0].value - 1].uid.ToString(),
				DateCreated = DateTime.Now.ToString(),
				DateModified = DateTime.Now.ToString(),
				CourseCode = fileInfo.Length.ToString(),
				CourseName = this.CourseName,
				ActiveHole = 0,
				RoundStatus = 1,
				RoundType = 2,
				NumberOfPlayers = 1,
				RoundSettings = SecurityFuncs.EncryptString(JsonConvert.SerializeObject(this.newRoundScript.CreateRoundSettingsHash(), new JsonSerializerSettings
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				})),
				RoundData = null
			};
			try
			{
				this.mainGameControler.RoundDBID = this.db.ds.CreateRound(thisRound).ID;
				this.mainGameControler.DBRoundObject = this.db.ds.GetRoundByID(this.mainGameControler.RoundDBID);
			}
			catch
			{
			}
		}
		else if (this.mainGameControler.RoundDBID > 0)
		{
			this.mainGameControler.DBRoundObject = this.db.ds.GetRoundByID(this.mainGameControler.RoundDBID);
		}
		this.splash.enabled = true;
		this.newRndPP.LoadGameVariables();
		this.StartRoundCheckOnlineAndSetup();
		this.mainGameControler.trajectory.HLACorrection = this.newRoundScript.HLACorrectEnable.isOn;
		if (this.ValidateCourseFileStruct(dirPath))
		{
			this.menuOrchestrator.LoadAssetBundleFromPathInit(text);
		}
	}

	protected void StartRoundCheckOnlineAndSetup()
	{
		if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsConnected)
		{
			PlayerData[] array = new PlayerData[8];
			int num = 0;
			this.mainGameControler.PlayerCount = 0;
			foreach (PlayerRunTimeData playerRunTimeData in this.mainGameControler.OnlinePlayers)
			{
				this.mainGameControler.player_a[num].PlayerID = playerRunTimeData.PlayerID;
				this.mainGameControler.player_a[num].Name = playerRunTimeData.Name;
				this.mainGameControler.player_a[num].hcp = playerRunTimeData.hcp;
				this.mainGameControler.player_a[num].prefLength = playerRunTimeData.prefLength;
				this.mainGameControler.player_a[num].prefTee = playerRunTimeData.prefTee;
				this.mainGameControler.player_a[num].hand = playerRunTimeData.hand;
				this.mainGameControler.player_a[num].valid = true;
				this.mainGameControler.player_a[num].Realism = playerRunTimeData.Realism;
				this.mainGameControler.player_a[num].BallCurvature = playerRunTimeData.BallCurvature;
				this.mainGameControler.player_a[num].ShootBoostType = playerRunTimeData.ShootBoostType;
				this.mainGameControler.player_a[num].PlayMode = playerRunTimeData.PlayMode;
				this.mainGameControler.player_a[num].ShotBoost = playerRunTimeData.ShotBoost;
				this.mainGameControler.player_a[num].color = ((!this.mainGameControler.GameModeID.Equals(0) && !this.mainGameControler.GameModeID.Equals(5)) ? playerRunTimeData.color : num);
				this.mainGameControler.player_a[num].OnlineUserID = playerRunTimeData.OnlineUserID;
				this.mainGameControler.PlayerCount++;
				this.mainGameControler.player_a[num].selectedPlayerListIndex = num;
				this.mainGameControler.player_a[num].selectedTeeIndex = playerRunTimeData.selectedTeeIndex;
				if (playerRunTimeData.selectedTeeIndex + 1 == 0)
				{
					switch (this.newRndPP.TeeGlobalSelect.value)
					{
					case 0:
						this.mainGameControler.player_a[num].selectedTeeIndex = 0;
						this.mainGameControler.player_a[num].selectedTeeName = this.newRndPP.parseDescFile.teeName[0];
						this.mainGameControler.player_a[num].surface = 18;
						this.mainGameControler.player_a[num].isOnTee = true;
						break;
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
					case 7:
					case 8:
						this.mainGameControler.player_a[num].selectedTeeIndex = this.newRndPP.TeeGlobalSelect.value;
						this.mainGameControler.player_a[num].selectedTeeName = this.newRndPP.parseDescFile.teeName[this.mainGameControler.player_a[num].selectedTeeIndex];
						this.mainGameControler.player_a[num].surface = 18;
						this.mainGameControler.player_a[num].isOnTee = true;
						break;
					default:
						this.mainGameControler.player_a[num].selectedTeeIndex = 0;
						this.mainGameControler.player_a[num].selectedTeeName = this.newRndPP.parseDescFile.teeName[0];
						this.mainGameControler.player_a[num].surface = 18;
						this.mainGameControler.player_a[num].isOnTee = true;
						break;
					}
				}
				else
				{
					if (playerRunTimeData.selectedTeeIndex + 1 > 0)
					{
						this.mainGameControler.player_a[num].selectedTeeIndex = playerRunTimeData.selectedTeeIndex;
					}
					this.mainGameControler.player_a[num].selectedTeeName = this.newRndPP.parseDescFile.teeName[this.mainGameControler.player_a[num].selectedTeeIndex];
					this.mainGameControler.player_a[num].surface = 18;
					this.mainGameControler.player_a[num].isOnTee = true;
				}
				num++;
			}
			string empty = string.Empty;
		}
		if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsMasterClient)
		{
			RaiseEventOptions raiseEventOptions = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.Others
			};
			bool flag = PhotonNetwork.RaiseEvent(OnlinePlayHelper.ClientsStartRound, "0", raiseEventOptions, SendOptions.SendReliable);
		}
	}

	protected void LoadProTip()
	{
		List<string> list = new List<string>();
		list.Add("Hit the L key to bring up the lighting customization panel.");
		list.Add("Stuck in the woods? Try a sim drop!");
		list.Add("Want to follow the professional tour? Join SimulatorGolfTour.com !");
		list.Add("Select show online courses to download more courses!");
		list.Add("Penalties are dynamic based on speed, lie, and launch angle.");
		list.Add("Understanding your lie is critical to predicting the shot outcome.");
		list.Add("Want to practice a single shot? Try On Course Practice mode!");
		list.Add("Customize the colors for your PC by using the Lighting Adjustment area.");
		list.Add("Hit F11 to enter or exit fullscreen mode.");
		list.Add("You can change what data is shown on any given data tile by clicking on it.");
		list.Add("Want immersiveness? Hit the H key!");
		list.Add("Using a laptop? Keep it plugged in for best performance.");
		list.Add("Hit \"L\" to easily adjust your left and right offset.");
		list.Add("Want to build a course? Join the community at zerosandonesgcd.com !");
		System.Random random = new System.Random();
		int index = random.Next(list.Count);
		this.TxtProTip.text = list[index];
	}

	public void LoadCourseLocal(int index)
	{
		this.mainGameControler.ActiveGameStartTime = DateTime.Now;
		this.newRoundMenu.SetActive(false);
		this.MenuGlobalWrapper.SetActive(false);
		this.courseLoadingScreen.SetActive(true);
		this.LoadProTip();
		string text;
		string dirPath;
		if (index == 1)
		{
			this.PracticeSettingsObj.ShotTraceCount_tgl(1);
			if (File.Exists(this.CourseFolderPath + "/GSPRange22/GSPRange22.unity3d"))
			{
				this.practiceMenu_go.SetActive(false);
				this.splash.GetComponent<UnityEngine.UI.Image>().sprite = (Sprite)Resources.Load("Sprites/DrivingRangeSplash", typeof(Sprite));
				this.CourseLoadingImage.sprite = (Sprite)Resources.Load("Sprites/DrivingRange_color_1440_984", typeof(Sprite));
				this.CourseLoadingCoureName.text = "Driving Range";
				text = this.CourseFolderPath + "/GSPRange22/GSPRange22.unity3d";
				this.PracticeFolderPath = this.CourseFolderPath + "/GSPRange22";
				string text2 = this.CourseFolderPath + "/GSPRange22/GSPRange22.GKD";
				dirPath = this.CourseFolderPath + "/GSPRange22";
				this.mainGameControler.db.ds.ClearDrivingRangeSession();
				this.parseDesc.ParseGKData(text2);
				this.ActiveGKDPath = text2;
			}
			else
			{
				this.practiceMenu_go.SetActive(false);
				this.splash.GetComponent<UnityEngine.UI.Image>().sprite = (Sprite)Resources.Load("Sprites/DrivingRangeSplash", typeof(Sprite));
				this.CourseLoadingImage.sprite = (Sprite)Resources.Load("Sprites/DrivingRange_color_1440_984", typeof(Sprite));
				this.CourseLoadingCoureName.text = "Driving Range";
				text = Application.dataPath + "/..//GSProRange/GSProRange.unity3d";
				this.PracticeFolderPath = Application.dataPath + "/../GSProRange";
				string text2 = Application.dataPath + "/..//GSProRange/GSProRange.gkd";
				dirPath = Application.dataPath + "/..//GSProRange";
				this.mainGameControler.db.ds.ClearDrivingRangeSession();
				this.parseDesc.ParseGKData(text2);
				this.ActiveGKDPath = text2;
			}
			this.mainGameControler.PlayerCount = 1;
			this.newRndPP.LoadGameVariables();
			this.mainGameControler.ActiveGameType = 255;
		}
		else if (index == 2)
		{
			this.practiceMenu_go.SetActive(false);
			this.splash.GetComponent<UnityEngine.UI.Image>().sprite = (Sprite)Resources.Load("Sprites/SkillsTestBtnColor", typeof(Sprite));
			this.CourseLoadingImage.sprite = (Sprite)Resources.Load("Sprites/SkillsTest1440_984", typeof(Sprite));
			this.CourseLoadingCoureName.text = "Practice Ground";
			text = this.CourseFolderPath + "/gsp_practicearea/gsp_practicearea.unity3d";
			this.PracticeFolderPath = this.CourseFolderPath + "/gsp_practicearea";
			if (File.Exists(text))
			{
				UnityEngine.Debug.Log("Practice Facility Found");
			}
			else
			{
				UnityEngine.Debug.LogWarning("Practice Facility NOT Found");
			}
			string text2 = this.CourseFolderPath + "/gsp_practicearea/gsp_practicearea.gkd";
			dirPath = this.CourseFolderPath + "/gsp_practicearea";
			this.mainGameControler.db.ds.ClearDrivingRangeSession();
			this.parseDesc.ParseGKData(text2);
			this.ActiveGKDPath = text2;
			this.mainGameControler.PlayerCount = 1;
			this.newRndPP.LoadGameVariables();
			this.mainGameControler.ActiveGameType = 253;
		}
		else
		{
			CourseRoundSettings courseRoundSettings = null;
			try
			{
				courseRoundSettings = this.db.ds.GetCourseSettingsCourseKey(this.CourseName);
			}
			catch
			{
				UnityEngine.Debug.LogWarning("SQLlite DLL load error - settings table does not exist");
			}
			if (courseRoundSettings != null)
			{
				courseRoundSettings.DateModified = DateTime.Now.ToString();
				courseRoundSettings.RoundSettings = SecurityFuncs.EncryptString(JsonConvert.SerializeObject(this.newRoundScript.CreateRoundSettingsHash(), new JsonSerializerSettings
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				}));
				if (!this.mainGameControler.ActiveGameIsTournament && !this.mainGameControler.IsCourseDesignerRecommendedSettings)
				{
					this.db.ds.UpdateCourseSettings(courseRoundSettings);
				}
			}
			else
			{
				courseRoundSettings = new CourseRoundSettings();
				courseRoundSettings.CourseName = this.CourseName;
				courseRoundSettings.CourseKey = this.CourseName;
				courseRoundSettings.DateModified = DateTime.Now.ToString();
				courseRoundSettings.RoundSettings = SecurityFuncs.EncryptString(JsonConvert.SerializeObject(this.newRoundScript.CreateRoundSettingsHash(), new JsonSerializerSettings
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				}));
				if (!this.mainGameControler.ActiveGameIsTournament && !this.mainGameControler.IsCourseDesignerRecommendedSettings)
				{
					this.db.ds.InsertCourseSettings(courseRoundSettings);
				}
			}
			string text2;
			if (this.mainGameControler.ActiveGameIsOnline && !PhotonNetwork.IsMasterClient)
			{
				text = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					".unity3d"
				});
				dirPath = this.CourseFolderPath + "/" + PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString();
				text2 = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					".gkd"
				});
				this.splash.GetComponent<UnityEngine.UI.Image>().sprite = (Sprite)Resources.Load("Sprites/Splash", typeof(Sprite));
				this.CourseLoadingImage.sprite = (Sprite)Resources.Load("Sprites/Splash", typeof(Sprite));
				this.CourseLoadingCoureName.text = "Online Play Guest";
			}
			else if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsMasterClient)
			{
				text = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					".unity3d"
				});
				dirPath = this.CourseFolderPath + "/" + PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString();
				text2 = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					"/",
					PhotonNetwork.CurrentRoom.CustomProperties["COURSE"].ToString(),
					".gkd"
				});
				this.splash.sprite = this.SelectedCourseImage.sprite;
				this.CourseLoadingImage.sprite = this.SelectedCourseImage.sprite;
				this.CourseLoadingCoureName.text = "Online Play Host";
			}
			else
			{
				text = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					this.CourseName,
					"/",
					this.CourseName,
					".unity3d"
				});
				dirPath = this.CourseFolderPath + "/" + this.CourseName;
				text2 = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					this.CourseName,
					"/",
					this.CourseName,
					".gkd"
				});
				this.splash.sprite = this.SelectedCourseImage.sprite;
				FileInfo fileInfo = new FileInfo(string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					this.CourseName,
					"/",
					this.CourseName,
					".unity3d"
				}));
				if (this.mainGameControler.RoundIsNew)
				{
					Round thisRound = new Round
					{
						PlayerName = this.topPnl.globalSettings.PlayerData_a[this.newRoundScript.newRoundPP.PlayerDDa[0].value - 1].Name,
						PlayerID = this.topPnl.globalSettings.PlayerData_a[this.newRoundScript.newRoundPP.PlayerDDa[0].value - 1].uid.ToString(),
						DateCreated = DateTime.Now.ToString(),
						DateModified = DateTime.Now.ToString(),
						CourseCode = fileInfo.Length.ToString(),
						CourseName = this.CourseName,
						ActiveHole = 0,
						RoundStatus = 1,
						RoundType = 0,
						NumberOfPlayers = 1,
						RoundSettings = SecurityFuncs.EncryptString(JsonConvert.SerializeObject(this.newRoundScript.CreateRoundSettingsHash(), new JsonSerializerSettings
						{
							ReferenceLoopHandling = ReferenceLoopHandling.Ignore
						})),
						RoundData = null
					};
					try
					{
						this.mainGameControler.RoundDBID = this.db.ds.CreateRound(thisRound).ID;
						this.mainGameControler.DBRoundObject = this.db.ds.GetRoundByID(this.mainGameControler.RoundDBID);
					}
					catch
					{
					}
				}
				else if (this.mainGameControler.RoundDBID > 0)
				{
					this.mainGameControler.DBRoundObject = this.db.ds.GetRoundByID(this.mainGameControler.RoundDBID);
				}
			}
			if (File.Exists(text2))
			{
				UnityEngine.Debug.Log("Loading Course file " + text2);
				this.parseDesc.ParseGKData(text2);
				this.ActiveGKDPath = text2;
			}
			else
			{
				UnityEngine.Debug.LogWarning("Loading file " + text + " failed");
			}
		}
		this.splash.enabled = true;
		this.newRoundScript.SetupGameVariables();
		this.newRndPP.LoadGameVariables();
		this.mainGameControler.gameMode.CalculateHCP();
		this.StartRoundCheckOnlineAndSetup();
		if (this.ValidateCourseFileStruct(dirPath))
		{
			this.menuOrchestrator.LoadAssetBundleFromPathInit(text);
		}
	}

	public bool ValidateCourseFileStruct(string dirPath)
	{
		if (!Directory.Exists(dirPath))
		{
			this.DisplayCourseError("Error loading course. Corrupt or unsupported.", 1);
			return false;
		}
		bool flag = false;
		foreach (string path in Directory.GetFiles(dirPath))
		{
			if (Path.GetExtension(path).ToLower().Contains(".description"))
			{
				flag = true;
			}
		}
		if (flag)
		{
			this.DisplayCourseError("Error loading course. Only GSPro courses are supported.", 1);
			return false;
		}
		return true;
	}

	private IEnumerator LoadAssetBundleFromPath(string path)
	{
		this.ProgressTxt.gameObject.SetActive(true);
		this.ProgressTxt.text = "Course Initializing, please wait.";
		this.CourseLoadingProgressText.text = "Course Initializing, please wait.";
		using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle("file://" + path, 1u, 0u))
		{
			yield return uwr.SendWebRequest();
			try
			{
				this.mainGameControler.SelectedCoursePath = path;
				this.assetBundle = DownloadHandlerAssetBundle.GetContent(uwr);
				string[] allScenePaths = this.assetBundle.GetAllScenePaths();
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(allScenePaths[0]);
				GameObject target = GameObject.Find("MainGameObjects");
				UnityEngine.Object.DontDestroyOnLoad(target);
				this.HomeMenuBackground.enabled = false;
				this.topMenu.SetActive(false);
				base.StartCoroutine(this.LoadScene(fileNameWithoutExtension));
				this.mainGameControler.LoadedCourse = fileNameWithoutExtension;
				this.playMenu.enabled = true;
				this.playMenu.gameObject.SetActive(true);
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.Log("Error loading course");
				this.DisplayCourseError("Error loading course. Please confirm course is not corrupt and created for the correct GSPro version.", 1);
			}
		}
		yield break;
	}

	public void ShowSGTDialog()
	{
		IEnumerable<Button> enumerable = from x in this.SGTMessageDialog.GetComponentsInChildren<Button>()
		where x.name.Equals("btnSignUp")
		select x;
		foreach (Button button in enumerable)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(delegate()
			{
				Application.OpenURL("https://simulatorgolftour.com/");
			});
		}
		this.SGTMessageDialog.SetActive(true);
	}

	public void DisplayCourseError(string errorMessage, int ButtonAction)
	{
		Text[] componentsInChildren = this.mainGameControler.MessageDialog.GetComponentsInChildren<Text>();
		if (componentsInChildren != null)
		{
			componentsInChildren.FirstOrDefault<Text>().text = errorMessage;
		}
		foreach (Button button in this.mainGameControler.MessageDialog.GetComponentsInChildren<Button>(true))
		{
			if (ButtonAction == 0)
			{
				button.gameObject.SetActive(false);
			}
			else if (ButtonAction == 1)
			{
				if (button != null)
				{
					button.gameObject.SetActive(true);
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate()
					{
						SceneManager.LoadScene("Reset");
						SceneManager.LoadScene("TVG1");
						this.LeaderboardMarquee.StartMarquee();
						this.MainMenuMoveIn();
					});
				}
			}
			else if (ButtonAction == 2)
			{
				button.gameObject.SetActive(true);
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(delegate()
				{
					this.mainGameControler.MessageDialog.SetActive(false);
				});
			}
			else if (ButtonAction == 3)
			{
				button.gameObject.SetActive(true);
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(delegate()
				{
					this.mainGameControler.parseDescFile.homeM.topPnl.KillGame_click();
				});
			}
			else if (ButtonAction == 4)
			{
				button.gameObject.SetActive(true);
				button.onClick.RemoveAllListeners();
				button.GetComponentInChildren<Text>().text = "Download";
				button.onClick.AddListener(delegate()
				{
					Application.OpenURL("https://aka.ms/vs/16/release/vc_redist.x64.exe");
					this.mainGameControler.parseDescFile.homeM.topPnl.KillGame_click();
				});
			}
		}
		this.mainGameControler.MessageDialog.SetActive(true);
	}

	public void GoToChangeLog()
	{
		Application.OpenURL("https://gsprogolf.com/change-log.html");
	}

	public void GoToOPCD()
	{
		Application.OpenURL("https://zerosandonesgcd.com/");
	}

	public void ReloadCourses()
	{
		this.BtnReloadCourses.GetComponentInChildren<Text>().text = "Loading...";
		this.HasCoursesInitCompleted = false;
		this.CourseInitInProgress = false;
		this.CourseInitLastStepCompleted = string.Empty;
	}

	public void ResetConnectConnection()
	{
		UnityEngine.Debug.Log("ResetConnectConnection");
		try
		{
			this.mainGameControler.tcpServ.tcpListener.Stop();
			this.mainGameControler.tcpServ.isShuttingDown = true;
			this.mainGameControler.tcpServAlt.tcpListener.Stop();
			this.mainGameControler.tcpServAlt.isShuttingDown = true;
			this.mainGameControler.BallState = 0;
			this.mainGameControler.EnableShot();
			this.mainGameControler.KillAllConnect();
			this.mainGameControler.StartConnect();
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.Log("ResetConnectConnection Failed");
		}
	}

	protected bool RoundSettingsAreValid()
	{
		if (this.newRndPP.PlayerDDa[0].value == 0)
		{
			Text[] componentsInChildren = this.mainGameControler.MessageDialog.GetComponentsInChildren<Text>();
			if (componentsInChildren != null)
			{
				componentsInChildren.FirstOrDefault<Text>().text = "Please select a valid first player to begin";
			}
			this.mainGameControler.MessageDialog.SetActive(true);
			return false;
		}
		bool flag = false;
		for (int i = 0; i < 18; i++)
		{
			if (this.newRoundScript.HoleSelect[i].isOn)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Text[] componentsInChildren2 = this.mainGameControler.MessageDialog.GetComponentsInChildren<Text>();
			if (componentsInChildren2 != null)
			{
				componentsInChildren2.FirstOrDefault<Text>().text = "Must have at least 1 hole enabled";
			}
			this.mainGameControler.MessageDialog.SetActive(true);
			return false;
		}
		List<int> list = new List<int>();
		if (this.newRoundScript.GameTypeDD.value == 2)
		{
			if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsConnected)
			{
				foreach (PlayerRunTimeData playerRunTimeData in this.mainGameControler.OnlinePlayers)
				{
					if (!list.Contains(playerRunTimeData.color))
					{
						list.Add(playerRunTimeData.color);
					}
				}
			}
			else
			{
				for (int j = 0; j < 8; j++)
				{
					if (this.newRoundScript.newRoundPP.PlayerDDa[j].value != 0 && !list.Contains(this.mainGameControler.player_a[j].color))
					{
						list.Add(this.mainGameControler.player_a[j].color);
					}
				}
			}
			if (list.Count == 2)
			{
				return true;
			}
			Text[] componentsInChildren3 = this.mainGameControler.MessageDialog.GetComponentsInChildren<Text>();
			if (componentsInChildren3 != null)
			{
				componentsInChildren3.FirstOrDefault<Text>().text = "Match Play requires 2 teams/colors";
			}
			this.mainGameControler.MessageDialog.SetActive(true);
			return false;
		}
		else if (this.newRoundScript.GameTypeDD.value == 3)
		{
			if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsConnected)
			{
				foreach (PlayerRunTimeData playerRunTimeData2 in this.mainGameControler.OnlinePlayers)
				{
					if (!list.Contains(playerRunTimeData2.color))
					{
						list.Add(playerRunTimeData2.color);
					}
				}
			}
			else
			{
				for (int k = 0; k < 8; k++)
				{
					if (this.newRoundScript.newRoundPP.PlayerDDa[k].value != 0 && !list.Contains(this.mainGameControler.player_a[k].color))
					{
						list.Add(this.mainGameControler.player_a[k].color);
					}
				}
			}
			bool flag2 = false;
			using (List<int>.Enumerator enumerator3 = list.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					int ThisColor = enumerator3.Current;
					if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsConnected)
					{
						if ((from x in this.mainGameControler.OnlinePlayers
						where x.color.Equals(ThisColor)
						select x).Count<PlayerRunTimeData>() != 2)
						{
							flag2 = true;
						}
					}
					else if ((from x in this.mainGameControler.player_a
					where x.color.Equals(ThisColor)
					select x).Count<PlayerRunTimeData>() != 2)
					{
						flag2 = true;
					}
				}
			}
			if (!flag2)
			{
				return true;
			}
			Text[] componentsInChildren4 = this.mainGameControler.MessageDialog.GetComponentsInChildren<Text>();
			if (componentsInChildren4 != null)
			{
				componentsInChildren4.FirstOrDefault<Text>().text = "Alternate shot requires teams of 2";
			}
			this.mainGameControler.MessageDialog.SetActive(true);
			return false;
		}
		else
		{
			if (this.newRoundScript.GameTypeDD.value == 1)
			{
				if (this.mainGameControler.ActiveGameIsOnline && PhotonNetwork.IsConnected)
				{
					foreach (PlayerRunTimeData playerRunTimeData3 in this.mainGameControler.OnlinePlayers)
					{
						if (list.Contains(playerRunTimeData3.color))
						{
							return true;
						}
						list.Add(playerRunTimeData3.color);
					}
				}
				else
				{
					for (int l = 0; l < 8; l++)
					{
						if (this.newRoundScript.newRoundPP.PlayerDDa[l].value != 0)
						{
							if (list.Contains(this.mainGameControler.player_a[l].color))
							{
								return true;
							}
							list.Add(this.mainGameControler.player_a[l].color);
						}
					}
				}
				Text[] componentsInChildren5 = this.mainGameControler.MessageDialog.GetComponentsInChildren<Text>();
				if (componentsInChildren5 != null)
				{
					componentsInChildren5.FirstOrDefault<Text>().text = "Scramble requires at least 1 team of 2 of more people";
				}
				this.mainGameControler.MessageDialog.SetActive(true);
				return false;
			}
			return true;
		}
	}

	public void LoadCourse(int index)
	{
		this.mainGameControler.ActiveGameStartTime = DateTime.Now;
		if (index == -1)
		{
			this.mainGameControler.ActiveGameCourseRangeWarmUp = true;
			index = 0;
		}
		this.mainGameControler.wctrl.NewRoundSetVariables();
		this.parseDesc.loadT.start = Time.realtimeSinceStartup;
		if (index == 1)
		{
			this.LoadCourseLocal(index);
		}
		else if (index == 2)
		{
			this.mainGameControler.SetMiniMapAndPenaltyForActivePlayer(0);
			string path = this.CourseFolderPath + "/gsp_practicearea/gsp_practicearea.unity3d";
			if (File.Exists(path))
			{
				UnityEngine.Debug.Log("Practice Facility Found");
				this.LoadCourseLocal(index);
			}
			else
			{
				UnityEngine.Debug.LogWarning("Practice Facility NOT Found");
				this.courseManaager.AddCourseToLocalDB("GSProPracticeFacility", true);
				this.DisplayCourseError("Practice Course Missing. Practice Course Facility Added to Download Queue", 2);
			}
		}
		else if (this.mainGameControler.ActiveGameIsTournament)
		{
			this.LoadCourseTournament(this.mainGameControler.TournamentCourseIndex, this.mainGameControler.TournamentPlayingIdx);
		}
		else if (this.RoundSettingsAreValid())
		{
			this.LoadCourseLocal(index);
		}
	}

	private void populateTeeSelectionDD()
	{
	}

	public void GlobalBackButtonClick()
	{
		if (this.ActiveMenuObject == this.homeMenu_go)
		{
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
			this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
		}
		else if (this.ActiveMenuObject == this.selectCourseMenu_go)
		{
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
			this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
			this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		}
		else if (this.ActiveMenuObject == this.newRoundMenu)
		{
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
			this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
			this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		}
		else if (this.ActiveMenuObject == this.practiceMenu_go)
		{
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
			this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
			this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		}
		else if (this.ActiveMenuObject == this.GlobalSettings)
		{
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
			this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
			this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		}
		if (this.NavigationBackObject == this.homeMenu_go)
		{
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
		}
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.ActiveMenuObject;
		this.FadeInOutLib.FadeToBlack();
	}

	public void img_click_index(string courseSelected, string CourseFriendlyName)
	{
		courseSelected = courseSelected.ToLower();
		if (CourseFriendlyName != string.Empty)
		{
			this.PlayButtonText.text = "Play " + CourseFriendlyName + "!";
		}
		else
		{
			this.PlayButtonText.text = "Play!";
		}
		UnityEngine.Debug.Log(string.Format("Selected Course: {0}", courseSelected));
		this.CourseName = courseSelected;
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = this.selectCourseMenu_go;
		this.FadeInOutLib.GameObjectToFadeIn = this.newRoundMenu;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.OnlinePlayMenu_go);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = this.selectCourseMenu_go;
		this.ActiveMenuObject = this.newRoundMenu;
		string text = string.Empty;
		if (this.mainGameControler.ActiveGameIsOnline && !PhotonNetwork.IsMasterClient)
		{
			if (Directory.Exists(this.CourseFolderPath))
			{
				text = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					courseSelected,
					"/",
					courseSelected,
					".gkd"
				});
				this.newRndPP.BundlePlayersForOnlinePlay();
			}
			else
			{
				UnityEngine.Debug.LogWarning("The path to the course folder is wrong(" + this.CourseFolderPath + ")");
			}
		}
		else if (this.FullCoursePathNoExtension != string.Empty)
		{
			text = this.FullCoursePathNoExtension + ".gkd";
		}
		else
		{
			UnityEngine.Debug.Log("Loading standard coure proc");
			if (Directory.Exists(this.CourseFolderPath))
			{
				text = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					courseSelected,
					"/",
					courseSelected,
					".gkd"
				});
				this.FullCoursePathNoExtension = string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					courseSelected,
					"/",
					courseSelected
				});
				this.CourseName = courseSelected;
			}
			else
			{
				UnityEngine.Debug.LogWarning("The path to the course folder is wrong(" + this.CourseFolderPath + ")");
			}
		}
		if (File.Exists(text))
		{
			UnityEngine.Debug.Log("Loading Course file " + text);
			this.parseDesc.ParseGKData(text);
			this.ActiveGKDPath = text;
		}
		else
		{
			UnityEngine.Debug.Log("GKD not found");
		}
		if (!(this.FullCoursePathNoExtension != string.Empty) || !this.mainGameControler.ActiveGameIsTournament)
		{
			this.populateTeeSelectionDD();
			this.SelectedCourseImage.sprite = this.SplashTextureDictionary[courseSelected];
			this.CourseLoadingImage.sprite = this.SplashTextureDictionary[courseSelected];
			this.CourseLoadingCoureName.text = CourseFriendlyName;
			this.newRndPP.updatePanels();
			this.newRoundScript.SetPracticeMode();
			this.newRoundScript.EnableDDs();
		}
		FileInfo fileInfo = new FileInfo(string.Concat(new string[]
		{
			this.CourseFolderPath,
			"/",
			courseSelected,
			"/",
			courseSelected,
			".unity3d"
		}));
		Round round = null;
		try
		{
			if (this.mainGameControler.ActiveGameIsTournament)
			{
				round = this.db.ds.GetLatestRoundByCourseCode(fileInfo.Length.ToString(), 2);
			}
			else
			{
				round = this.db.ds.GetLatestRoundByCourseCode(fileInfo.Length.ToString(), 0);
			}
		}
		catch
		{
			UnityEngine.Debug.LogWarning("SQLlite DLL load error");
		}
		if (round != null)
		{
			UnityEngine.Debug.Log("DB Round Found: " + round.ID.ToString());
			if (round.RoundStatus == 1 && !this.mainGameControler.ActiveGameIsOnline)
			{
				this.mainGameControler.PerviousPlayableRoundFound = true;
				this.newRoundScript.PanelResumeRound.SetActive(true);
				this.mainGameControler.RoundDBID = round.ID;
			}
			else
			{
				this.mainGameControler.PerviousPlayableRoundFound = false;
				this.newRoundScript.ResumeRound.isOn = false;
				this.newRoundScript.PanelResumeRound.SetActive(false);
			}
		}
		else
		{
			this.mainGameControler.PerviousPlayableRoundFound = false;
			this.newRoundScript.ResumeRound.isOn = false;
			this.newRoundScript.PanelResumeRound.SetActive(false);
		}
		if (this.mainGameControler.ActiveGameIsOnline)
		{
			if (PhotonNetwork.IsMasterClient)
			{
				this.newRoundScript.PlayModeDD.value = 1;
				FileInfo fileInfo2 = new FileInfo(string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					this.CourseName,
					"/",
					this.CourseName,
					".unity3d"
				}));
				FileInfo fileInfo3 = new FileInfo(string.Concat(new string[]
				{
					this.CourseFolderPath,
					"/",
					this.CourseName,
					"/",
					this.CourseName,
					".GKD"
				}));
				PhotonNetwork.CurrentRoom.IsOpen = true;
				ExitGames.Client.Photon.Hashtable propertiesToSet = new ExitGames.Client.Photon.Hashtable
				{
					{
						"COURSE",
						this.CourseName
					},
					{
						"COURSENAME",
						this.mainGameControler.parseDescFile.sCname
					},
					{
						"COURSESIZE",
						fileInfo2.Length + fileInfo3.Length
					},
					{
						"GSProVersion",
						this.mainGameControler.GSProVersion.text
					},
					{
						"ActiveGameIsTournament",
						this.mainGameControler.ActiveGameIsTournament
					}
				};
				PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToSet, null, null);
				if (this.mainGameControler.ActiveGameIsTournament && this.mainGameControler.TournamentID > 0)
				{
					ExitGames.Client.Photon.Hashtable propertiesToSet2 = new ExitGames.Client.Photon.Hashtable
					{
						{
							"ActiveGameIsTournament",
							this.mainGameControler.ActiveGameIsTournament
						},
						{
							"TournamentID",
							this.mainGameControler.TournamentID
						},
						{
							"TournamentRound",
							this.tourMnu.ListTournaments[this.tourMnu.ActiveTournament].Round
						}
					};
					PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToSet2, null, null);
				}
			}
			else
			{
				this.newRoundScript.DisableDDs();
			}
		}
		else if (this.mainGameControler.ActiveGameIsTournament)
		{
			this.newRoundScript.DisableDDs();
		}
		else
		{
			CourseRoundSettings courseRoundSettings = null;
			try
			{
				courseRoundSettings = this.db.ds.GetCourseSettingsCourseKey(courseSelected);
			}
			catch
			{
				UnityEngine.Debug.LogWarning("SQLlite DLL load error - settings table does not exist");
			}
			if (courseRoundSettings != null)
			{
				this.newRoundScript.LoadCourseSettingsFromDB(courseRoundSettings);
			}
			else
			{
				try
				{
					courseRoundSettings = this.db.ds.GetLatestCourseSettings();
				}
				catch
				{
					UnityEngine.Debug.LogWarning("SQLlite DLL load error - settings table does not exist");
				}
				if (courseRoundSettings != null)
				{
					this.newRoundScript.LoadCourseSettingsFromDB(courseRoundSettings);
				}
			}
		}
	}

	public void newRoundMoveIn()
	{
		if (PhotonNetwork.IsConnected && this.mainGameControler.ActiveGameIsOnline)
		{
			this.newRoundScript.InvitePlayers.SetActive(false);
		}
		LeanTween.moveX(this.newRoundMenu, 3640f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
		this.newRoundMenuIsOut = true;
	}

	public void newRoundMoveOut()
	{
		LeanTween.moveX(this.newRoundMenu, 7480f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
		this.newRoundMenuIsOut = true;
	}

	public void MainMenuInit()
	{
		this.LeaderboardMarquee.gameObject.SetActive(true);
		this.mainGameControler.EndRoundCleanUp();
		this.courseManaager.ClearDownloadingCourses();
		this.courseManaager.GetCourseRepos();
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
		}
		this.MainMenuIsOut = true;
	}

	public void MainMenuMoveIn()
	{
		this.LeaderboardMarquee.gameObject.SetActive(true);
		this.mainGameControler.EndRoundCleanUp();
		this.courseManaager.ClearDownloadingCourses();
		this.courseManaager.GetCourseRepos();
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
		}
		this.MainMenuIsOut = true;
		this.FadeInOutLib.SetFadeTimesToMenu();
		this.FadeInOutLib.GameObjectToFadeAway = null;
		this.FadeInOutLib.GameObjectToFadeIn = this.homeMenu_go;
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.MenuGlobalWrapper);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.pnlEndRound);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.selectCourseMenu_go);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.practiceMenu_go);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.TournamentMenu_go);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.OnlinePlayMenu_go);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalSettings);
		this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnBackButton);
		this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnExit);
		this.FadeInOutLib.FadeToBlack();
		this.NavigationBackObject = null;
		this.ActiveMenuObject = this.homeMenu_go;
	}

	public void MainMenuMoveOut()
	{
		this.LeaderboardMarquee.gameObject.SetActive(false);
		LeanTween.moveX(this.homeMenu_go, -2500f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
		this.MainMenuIsOut = true;
	}

	public void SelectCourseMoveIn()
	{
		if (this.courseCount < 1)
		{
			this.DisplayCourseError("No courses found. Go to Settings -> Courses to add courses.", 2);
		}
		LeanTween.moveX(this.selectCourseMenu_go, 480f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
		this.selectMenuIsOut = false;
	}

	public void SelectCourseMoveOut()
	{
		LeanTween.moveX(this.selectCourseMenu_go, 4300f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
		this.selectMenuIsOut = true;
	}

	public void PracticeMenuMoveIn()
	{
		this.practiceMenu_go.transform.position = new Vector3(5760f * base.GetComponentInParent<Canvas>().scaleFactor, this.practiceMenu_go.transform.position.y, this.practiceMenu_go.transform.position.z);
		LeanTween.moveX(this.practiceMenu_go, 1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void TournamentMenuMoveIn()
	{
		this.TournamentMenu_go.transform.position = new Vector3(5760f * base.GetComponentInParent<Canvas>().scaleFactor, this.TournamentMenu_go.transform.position.y, this.TournamentMenu_go.transform.position.z);
		LeanTween.moveX(this.TournamentMenu_go, 1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void OnlinePlayMenuMoveIn()
	{
		this.OnlinePlayMenu_go.transform.position = new Vector3(5760f * base.GetComponentInParent<Canvas>().scaleFactor, this.OnlinePlayMenu_go.transform.position.y, this.OnlinePlayMenu_go.transform.position.z);
		LeanTween.moveX(this.OnlinePlayMenu_go, 1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void OnlinePlayMenuMoveInFromLeft()
	{
		this.OnlinePlayMenu_go.transform.position = new Vector3(-3840f * base.GetComponentInParent<Canvas>().scaleFactor, this.OnlinePlayMenu_go.transform.position.y, this.OnlinePlayMenu_go.transform.position.z);
		LeanTween.moveX(this.OnlinePlayMenu_go, 1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void TournamentMenuMoveInFromLeft()
	{
		this.TournamentMenu_go.transform.position = new Vector3(-3840f * base.GetComponentInParent<Canvas>().scaleFactor, this.TournamentMenu_go.transform.position.y, this.TournamentMenu_go.transform.position.z);
		LeanTween.moveX(this.TournamentMenu_go, 1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void TournamentMenuMoveOut()
	{
		LeanTween.moveX(this.TournamentMenu_go, 5760f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void OnlinePlayMenuMoveOut()
	{
		LeanTween.moveX(this.OnlinePlayMenu_go, 5760f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void OnlinePlayMenuMoveOutLeft()
	{
		LeanTween.moveX(this.OnlinePlayMenu_go, -4000f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void TournamentMenuMoveOutLeft()
	{
		LeanTween.moveX(this.TournamentMenu_go, -3840f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void PracticeMenuMoveOut()
	{
		LeanTween.moveX(this.practiceMenu_go, 5760f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void PracticeMenuMoveOutLeft()
	{
		LeanTween.moveX(this.practiceMenu_go, -1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void PracticeMenuMoveInFromLeft()
	{
		this.practiceMenu_go.transform.position = new Vector3(-1920f * base.GetComponentInParent<Canvas>().scaleFactor, this.practiceMenu_go.transform.position.y, this.practiceMenu_go.transform.position.z);
		LeanTween.moveX(this.practiceMenu_go, 1920f * base.GetComponentInParent<Canvas>().scaleFactor, 0.4f);
	}

	public void SettingBtn_click()
	{
		this.topPnl.gs.HoleIntroTgl.interactable = true;
		this.topPnl.gs.GreenToTeeTgl.interactable = true;
		if (!this.GlobalSettings.activeSelf)
		{
			this.topPnl.BtnPlayers.gameObject.transform.parent.gameObject.SetActive(true);
			this.topPnl.BtnGame.gameObject.transform.parent.gameObject.SetActive(true);
			this.topPnl.BtnActiveRound.gameObject.transform.parent.gameObject.SetActive(false);
			this.topPnl.BtnActiveRoundPlayers.gameObject.transform.parent.gameObject.SetActive(false);
			this.topPnl.BtnOffset.gameObject.transform.parent.gameObject.SetActive(true);
			this.topPnl.BtnPractice.gameObject.transform.parent.gameObject.SetActive(false);
			if (this.ActiveMenuObject == null)
			{
				this.ActiveMenuObject = this.homeMenu_go;
			}
			this.GameObjectPriorToSettings = this.ActiveMenuObject;
			this.topPnl.ToggleSettingsTab(1);
			this.RoundSettingsPanelButton.SetActive(false);
			this.FadeInOutLib.SetFadeTimesToMenu();
			this.FadeInOutLib.GameObjectToFadeAway = this.ActiveMenuObject;
			this.FadeInOutLib.GameObjectToFadeIn = this.GlobalSettings;
			this.FadeInOutLib.GameObjectsToShowOnTransition.Add(this.GlobalBtnBackButton);
			this.FadeInOutLib.GameObjectsToHideOnTransition.Add(this.GlobalBtnExit);
			this.FadeInOutLib.FadeToBlack();
			this.ActiveMenuObject = this.GlobalSettings;
			this.topPnl.PlayerBtn_click();
			this.topPnl.OffsetVisualEditorBtn.SetActive(false);
			this.RndMnuBtn.gameObject.SetActive(false);
			this.BtnEndRound.SetActive(false);
		}
	}

	private bool IsUnstable;

	public bool HasInitCompleted;

	public bool InitActionInProgress;

	public string InitLastStepCompleted = string.Empty;

	private Thread SystemCheckThead;

	private Thread CourseLoaderThead;

	public bool IsLicenseValid;

	public bool HasCoursesInitCompleted = true;

	public bool CourseInitInProgress = true;

	public string CourseInitLastStepCompleted = string.Empty;

	public string ActiveGKDPath = string.Empty;

	private List<CourseItemMetaData> AllCourseObjects = new List<CourseItemMetaData>();

	public List<CourseRepoCourse> ListCourseDataObject = new List<CourseRepoCourse>();

	public CourseScrollerController thisCourseScroller;

	public TournamentScrollerController thisTournamentScroller;

	public Toggle ToggleCourseShowOnline;

	public int CourseSortIndex;

	public bool CourseListingUseAltView;

	public UnityEngine.UI.Image BtnCourseGridViewImage;

	public UnityEngine.UI.Image BtnCourseListViewImage;

	public List<string> BlackListedCourses = new List<string>();

	public SuperTextMesh LoadingScreenMessage;

	public Canvas LoadingFlagCanvasHolder;

	public Canvas CanvasFlagHolderCTA;

	public UnityEngine.UI.Image ImageStaticLoading;

	public FadeInFadeOutHelper FadeInOutLib;

	public bool BoolSystemCheckIsDone;

	public bool CoursesLoaded;

	public GSPml extL;

	public MarqueeText LeaderboardMarquee;

	public CourseManager courseManaager;

	public SettingsTopPanel topPnl;

	public Canvas stngCanvas;

	public MsgBoxSetting msgBoxS;

	public NewRoundScript newRoundScript;

	public MainGameControler mainGameControler;

	public MenuOrchestrator menuOrchestrator;

	public TournamentMenu tourMnu;

	public OnlinePlay onlinePlayMenu;

	public NewRoundPlayerPanel newRndPP;

	public parseDesc parseDesc;

	public ActiveRoundData aRndData;

	public UnityEngine.UI.Image SelectedCourseImage;

	public GameObject selectCourseMenu;

	public GameObject newRoundMenu;

	public GameObject courseLoadingScreen;

	public UnityEngine.UI.Image splash;

	public UnityEngine.UI.Image HomeMenuBackground;

	public GameObject topMenu;

	public Text ProgressTxt;

	public Text TxtProTip;

	public Text CourseLoadingProgressText;

	public UnityEngine.UI.Image CourseLoadingImage;

	public SuperTextMesh CourseLoadingCoureName;

	public GameObject topMenu_go;

	public GameObject homeMenu_go;

	public GameObject selectCourseMenu_go;

	public GameObject practiceMenu_go;

	public GameObject TournamentMenu_go;

	public GameObject OnlinePlayMenu_go;

	public Scrollbar imgscroller;

	public GameObject MiniMapCampr;

	public GameObject MiniMapCanpr;

	public UnityEngine.UI.Image playMenu;

	public Canvas settingCanvas;

	public Button RndMnuBtn;

	public GameObject pnlEndRound;

	public GameObject BtnEndRound;

	public Dropdown teeDD;

	public dbContext db;

	public UnityEngine.UI.Image ci0;

	public UnityEngine.UI.Image ci1;

	public UnityEngine.UI.Image ci2;

	public UnityEngine.UI.Image ci3;

	public UnityEngine.UI.Image ci4;

	public UnityEngine.UI.Image ci5;

	public UnityEngine.UI.Image ci6;

	public UnityEngine.UI.Image ci7;

	public UnityEngine.UI.Image ci8;

	public Sprite DefaultCourseImage;

	public Text cit0;

	public Text cit1;

	public Text cit2;

	public Text cit3;

	public Text cit4;

	public Text cit5;

	public Text cit6;

	public Text cit7;

	public Text cit8;

	public Button BtnReloadCourses;

	public Button BtnClearCache;

	public Button BtnRestartConnect;

	public string hwui;

	private string pathS = "D:/Crs";

	private string pathSa = "C:/Crs";

	public string CourseFolderPath = string.Empty;

	public string FullCoursePathNoExtension = string.Empty;

	public string CourseName = string.Empty;

	public string PracticeFolderPath = string.Empty;

	private uint courseImageVisibleSlot0;

	private string sCD;

	private string sTD;

	private string sPD;

	private string sSD;

	private string sCname;

	private string sFolder;

	private string sSplash;

	private string sCauthor;

	private string sCvers;

	private string sCalt;

	public GameObject CourseSelectHolder;

	public GameObject CourseSelectSinglePreFab;

	public GameObject CourseListingHolderV2;

	public GameObject CourseListItemV2PreFab;

	public GameObject LicenseActionPanel;

	public Text LicenseActionMessage;

	public string LicenseKey;

	public GameObject GlobalBtnBackButton;

	public GameObject GlobalBtnExit;

	public GameObject GlobalBtnSettings;

	public GameObject NavigationBackObject;

	public GameObject ActiveMenuObject;

	public GameObject MenuGlobalWrapper;

	public GameObject GlobalSettings;

	public GameObject RoundSettingsPanelButton;

	public GameObject GameObjectPriorToSettings;

	public Sprite SoundOn;

	public Sprite SoundOff;

	public UnityEngine.UI.Image ImageMenuSound;

	public AudioSource MenuSound;

	private bool IsMenuSoundOn = true;

	public InputField CourseSearchInput;

	public GameObject RoundHistoryMainPnl;

	public GameObject SGTMessageDialog;

	public PracticeSettings PracticeSettingsObj;

	public SuperTextMesh PlayButtonText;

	private int teeTypeCnt;

	private string[] teeName = new string[16];

	public int courseCount;

	public List<string> namesOfCourses = new List<string>();

	public List<string> ActualNamesOfCourses = new List<string>();

	public List<Sprite> SplashTextures = new List<Sprite>();

	private string selectedPath = string.Empty;

	private bool fileLoadFailed;

	private string courseName;

	private string splashName;

	private string[] dirs = new string[]
	{
		string.Empty
	};

	public int failCount;

	private Texture2D BaseTex;

	private GolfCourse thisGolfCourse;

	public Dictionary<string, Sprite> SplashTextureDictionary = new Dictionary<string, Sprite>();

	public Dictionary<string, string> CourseNameDictionary = new Dictionary<string, string>();

	private List<UnityEngine.UI.Image> CIlist = new List<UnityEngine.UI.Image>();

	private List<Text> CITlist = new List<Text>();

	private Text[] CITa = new Text[9];

	private UnityEngine.UI.Image[] CIa = new UnityEngine.UI.Image[9];

	public int SelectedcourseIndex;

	public int loadProgress;

	public AssetBundle assetBundle;

	private bool MainMenuIsOut;

	private bool selectMenuIsOut;

	private bool newRoundMenuIsOut;

	private struct HoleTee
	{
		public string type;

		public int par;

		public int strkidx;

		public double w;

		public double h;

		public double X;

		public double Y;

		public double Z;

		public int hidx;

		public int orderidx;
	}
}
