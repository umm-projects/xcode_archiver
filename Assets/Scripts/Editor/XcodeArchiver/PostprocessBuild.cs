using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityModule.Settings;

namespace XcodeArchiver {

    public class PostprocessBuild : IPostprocessBuild {

        public const int POSTPROCESS_BUILD_CALLBACK_ORDER = 100;

        /// <summary>
        /// xcodebuild コマンドのパス
        /// </summary>
        private const string PATH_XCODEBUILD_BIN = "/usr/bin/xcodebuild";

        /// <summary>
        /// 出力オプション種別
        /// </summary>
        public enum ExportOptionType {
            AppStore,
            AdHoc,
            Enterprise,
            Development,
        }

        /// <summary>
        /// 出力オプションと plist のファイル名のマップ
        /// </summary>
        private static readonly Dictionary<ExportOptionType, string> EXPORT_OPTION_MAP = new Dictionary<ExportOptionType, string>() {
            { ExportOptionType.AppStore   , "app-store" },
            { ExportOptionType.AdHoc      , "ad-hoc" },
            { ExportOptionType.Enterprise , "enterprise" },
            { ExportOptionType.Development, "development" },
        };

        /// <summary>
        /// 出力オプションと出力先ディレクトリ名のマップ
        /// </summary>
        private static readonly Dictionary<ExportOptionType, string> EXPORT_DIRECTORY_MAP = new Dictionary<ExportOptionType, string>() {
            { ExportOptionType.AppStore, "export-app-store" },
            { ExportOptionType.AdHoc, "export-app-store" },
        };

        /// <summary>
        /// パスの実体
        /// </summary>
        private string exportedPath;

        /// <summary>
        /// パス
        /// </summary>
        public string ExportedPath {
            get {
                return this.exportedPath;
            }
            set {
                this.exportedPath = value;
            }
        }

        public int callbackOrder {
            get {
                return POSTPROCESS_BUILD_CALLBACK_ORDER;
            }
        }

        public void OnPostprocessBuild(BuildTarget target, string path) {
            if (target != BuildTarget.iOS) {
                return;
            }
            if (!EnvironmentSetting.Instance.ShouldRunXcodeArchive) {
                return;
            }
            this.ExportedPath = path;

            this.Prepare();
            this.ExecuteBuildAndArchive();
            this.ExecuteExport(ExportOptionType.AdHoc);
            if (EnvironmentSetting.Instance.ShouldExportAppStoreArchive && !EditorUserBuildSettings.development) {
                this.ExecuteExport(ExportOptionType.AppStore);
            }
        }

        private void Prepare() {
            this.GenerateExportOptionsPlist(ExportOptionType.AppStore);
            this.GenerateExportOptionsPlist(ExportOptionType.AdHoc);
        }

        private void ExecuteBuildAndArchive() {
            StringBuilder sb = new StringBuilder();
            if (EnvironmentSetting.Instance.UseXCWorkspace) {
                sb.AppendFormat(" -workspace \"{0}/Unity-iPhone.xcworkspace\"", this.ExportedPath);
            } else {
                sb.AppendFormat(" -project \"{0}/Unity-iPhone.xcodeproj\"", this.ExportedPath);
            }
            sb.AppendFormat(" -scheme \"Unity-iPhone\"");
            sb.AppendFormat(" -archivePath \"{0}/Unity-iPhone.xcarchive\" ", this.ExportedPath);
            sb.AppendFormat(" -sdk iphoneos");
            sb.AppendFormat(" -configuration Release");
            sb.AppendFormat(" clean archive");
            sb.AppendFormat(" CODE_SIGN_IDENTITY=\"iPhone Developer\"");
            sb.AppendFormat(" DEVELOPMENT_TEAM=\"{0}\"", PlayerSettings.iOS.appleDeveloperTeamID);
            if (!string.IsNullOrEmpty(PlayerSettings.iOS.iOSManualProvisioningProfileID)) {
                sb.AppendFormat(" PROVISIONING_PROFILE=\"{0}\"", PlayerSettings.iOS.iOSManualProvisioningProfileID);
            }
            System.Diagnostics.Process process = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = PATH_XCODEBUILD_BIN,
                    Arguments = sb.ToString(),
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        private void ExecuteExport(ExportOptionType exportOptionType) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(" -exportArchive");
            sb.AppendFormat(" -archivePath \"{0}/Unity-iPhone.xcarchive\" ", this.ExportedPath);
            sb.AppendFormat(" -exportPath \"{0}/{1}\"", this.ExportedPath, EXPORT_DIRECTORY_MAP[exportOptionType]);
            sb.AppendFormat(" -exportOptionsPlist \"{0}/{1}.plist\"", this.ExportedPath, EXPORT_OPTION_MAP[exportOptionType]);
            System.Diagnostics.Process process = new System.Diagnostics.Process {
                StartInfo = {
                    FileName = PATH_XCODEBUILD_BIN,
                    Arguments = sb.ToString(),
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        private void GenerateExportOptionsPlist(ExportOptionType exportOptionType) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.AppendFormat("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n");
            sb.AppendFormat("<plist version=\"1.0\">\n");
            sb.AppendFormat("<dict>\n");
            sb.AppendFormat("\t<key>method</key>\n");
            sb.AppendFormat("\t<string>{0}</string>\n", EXPORT_OPTION_MAP[exportOptionType]);
            // 一部の広告 SDK が Bitcode サポートしていないことがあったりする。
            sb.AppendFormat("\t<key>compileBitcode</key>\n");
            sb.AppendFormat("\t<false/>\n");
            // iOS のオンデマンドリソース対応はデフォルト Off で良いかと。
            sb.AppendFormat("\t<key>embedOnDemandResourcesAssetPacksInBundle</key>\n");
            sb.AppendFormat("\t<false/>\n");
            sb.AppendFormat("</dict>\n");
            sb.AppendFormat("</plist>\n");
            StreamWriter w = new StreamWriter(string.Format("{0}/{1}.plist", this.ExportedPath, EXPORT_OPTION_MAP[exportOptionType]));
            w.Write(sb.ToString());
            w.Close();
        }

    }

}