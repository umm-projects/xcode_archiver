﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
using UnityModule.Settings;

namespace XcodeArchiver
{
#if UNITY_2018_1_OR_NEWER
    [PublicAPI]
    public class PostprocessBuild : IPostprocessBuildWithReport
    {
#else
    [PublicAPI]
    public class PostprocessBuild : IPostprocessBuild
    {
#endif
        private const int PostprocessBuildCallbackOrder = 100;

        /// <summary>
        /// xcodebuild コマンドのパス
        /// </summary>
        private const string PathXcodebuildBin = "/usr/bin/xcodebuild";

        /// <summary>
        /// 出力オプション種別
        /// </summary>
        private enum ExportOptionType
        {
            AppStore,
            AdHoc,
            Enterprise,
            Development,
        }

        /// <summary>
        /// 出力オプションと plist のファイル名のマップ
        /// </summary>
        private static readonly Dictionary<ExportOptionType, string> ExportOptionMap = new Dictionary<ExportOptionType, string>()
        {
            {ExportOptionType.AppStore, "app-store"},
            {ExportOptionType.AdHoc, "ad-hoc"},
            {ExportOptionType.Enterprise, "enterprise"},
            {ExportOptionType.Development, "development"},
        };

        /// <summary>
        /// 出力オプションと出力先ディレクトリ名のマップ
        /// </summary>
        private static readonly Dictionary<ExportOptionType, string> ExportDirectoryMap = new Dictionary<ExportOptionType, string>()
        {
            {ExportOptionType.AppStore, "export-app-store"},
            {ExportOptionType.AdHoc, "export-ad-hoc"},
        };

        private string ExportedPath { get; set; }

        public int callbackOrder => PostprocessBuildCallbackOrder;

#if UNITY_2018_1_OR_NEWER
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            if (!XcodeArchiverSetting.GetOrDefault().ShouldRunXcodeArchive)
            {
                return;
            }

            ExportedPath = report.summary.outputPath;
#else
        public void OnPostprocessBuild(BuildTarget target, string path) {
            if (target != BuildTarget.iOS) {
                return;
            }
            if (!XcodeArchiverSetting.GetOrDefault().ShouldRunXcodeArchive) {
                return;
            }
            this.ExportedPath = path;
#endif

            Prepare();
            ExecuteBuildAndArchive();
            ExecuteExport(ExportOptionType.AdHoc);
            if (XcodeArchiverSetting.GetOrDefault().ShouldExportAppStoreArchive && !EditorUserBuildSettings.development)
            {
                ExecuteExport(ExportOptionType.AppStore);
            }
        }

        private void Prepare()
        {
            GenerateExportOptionsPlist(ExportOptionType.AppStore);
            GenerateExportOptionsPlist(ExportOptionType.AdHoc);
        }

        private void ExecuteBuildAndArchive()
        {
            var sb = new StringBuilder();
            // xcworkspace 利用可否は当該ディレクトリの有無で判定
            sb.AppendFormat(
                Directory.Exists($"{ExportedPath}/Unity-iPhone.xcworkspace")
                    ? " -workspace \"{0}/Unity-iPhone.xcworkspace\""
                    : " -project \"{0}/Unity-iPhone.xcodeproj\"",
                ExportedPath
            );
            sb.AppendFormat(" -scheme \"Unity-iPhone\"");
            sb.AppendFormat(" -archivePath \"{0}/Unity-iPhone.xcarchive\" ", ExportedPath);
            sb.AppendFormat(" -sdk iphoneos");
            sb.AppendFormat(" -configuration Release");
            sb.AppendFormat(" -allowProvisioningUpdates");
            sb.AppendFormat(" clean archive");
            sb.AppendFormat(" CODE_SIGN_IDENTITY=\"iPhone Developer\"");
            sb.AppendFormat(" DEVELOPMENT_TEAM=\"{0}\"", PlayerSettings.iOS.appleDeveloperTeamID);
            if (!string.IsNullOrEmpty(PlayerSettings.iOS.iOSManualProvisioningProfileID))
            {
                sb.AppendFormat(" PROVISIONING_PROFILE=\"{0}\"", PlayerSettings.iOS.iOSManualProvisioningProfileID);
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = PathXcodebuildBin,
                    Arguments = sb.ToString(),
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        private void ExecuteExport(ExportOptionType exportOptionType)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(" -exportArchive");
            sb.AppendFormat(" -archivePath \"{0}/Unity-iPhone.xcarchive\" ", ExportedPath);
            sb.AppendFormat(" -exportPath \"{0}/{1}\"", ExportedPath, ExportDirectoryMap[exportOptionType]);
            sb.AppendFormat(" -exportOptionsPlist \"{0}/{1}.plist\"", ExportedPath, ExportOptionMap[exportOptionType]);
            sb.AppendFormat(" -allowProvisioningUpdates");
            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = PathXcodebuildBin,
                    Arguments = sb.ToString(),
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        private void GenerateExportOptionsPlist(ExportOptionType exportOptionType)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.AppendFormat("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n");
            sb.AppendFormat("<plist version=\"1.0\">\n");
            sb.AppendFormat("<dict>\n");
            sb.AppendFormat("\t<key>method</key>\n");
            sb.AppendFormat("\t<string>{0}</string>\n", ExportOptionMap[exportOptionType]);
            // 一部の広告 SDK が Bitcode サポートしていないことがあったりする。
            sb.AppendFormat("\t<key>compileBitcode</key>\n");
            sb.AppendFormat("\t<false/>\n");
            // iOS のオンデマンドリソース対応はデフォルト Off で良いかと。
            sb.AppendFormat("\t<key>embedOnDemandResourcesAssetPacksInBundle</key>\n");
            sb.AppendFormat("\t<false/>\n");
            sb.AppendFormat("</dict>\n");
            sb.AppendFormat("</plist>\n");
            var w = new StreamWriter($"{ExportedPath}/{ExportOptionMap[exportOptionType]}.plist");
            w.Write(sb.ToString());
            w.Close();
        }
    }
}