using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;

namespace XcodeArchiver {

    public class PostprocessBuild : IPostprocessBuild {

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
                return 0;
            }
        }

        public void OnPostprocessBuild(BuildTarget target, string path) {
            if (target != BuildTarget.iOS) {
                return;
            }
            this.ExportedPath = path;

            this.Prepare();
            this.ExecuteBuild();
            this.ExecuteArchive();
        }

        private void Prepare() {
            this.GenerateExportOptionsPlist(ExportOptionType.AppStore);
            this.GenerateExportOptionsPlist(ExportOptionType.AdHoc);
        }

        private void ExecuteBuild() {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(" -project \"{0}/Unity-iPhone.xcodeproj\"", this.ExportedPath);
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

        private void ExecuteArchive() {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(" -exportArchive");
            sb.AppendFormat(" -archivePath \"{0}/Unity-iPhone.xcarchive\" ", this.ExportedPath);
            sb.AppendFormat(" -exportPath \"{0}/build\"", this.ExportedPath);
            sb.AppendFormat(" -exportOptionsPlist \"{0}/{1}.plist\"", this.ExportedPath, EXPORT_OPTION_MAP[ExportOptionType.AdHoc]);
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
            sb.AppendFormat("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendFormat("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
            sb.AppendFormat("<plist version=\"1.0\">");
            sb.AppendFormat("<dict>");
            sb.AppendFormat("\t<key>method</key>");
            sb.AppendFormat("\t<string>{0}</string>", EXPORT_OPTION_MAP[exportOptionType]);
            if (exportOptionType != ExportOptionType.AppStore) {
                // AppStore の場合であっても bitcode (中間言語生成) は Off にしといた方が安全かも…。
                // 一部の広告 SDK が Bitcode サポートしていないことがあったりする。
                sb.AppendFormat("\t<key>compileBitcode</key>");
                sb.AppendFormat("\t<false/>");
                // iOS のオンデマンドリソース対応はデフォルト Off で良いかと。
                sb.AppendFormat("\t<key>embedOnDemandResourcesAssetPacksInBundle</key>");
                sb.AppendFormat("\t<false/>");
            }
            sb.AppendFormat("</dict>");
            sb.AppendFormat("</plist>");
            StreamWriter w = new StreamWriter(string.Format("{0}/{1}.plist", this.ExportedPath, EXPORT_OPTION_MAP[exportOptionType]));
            w.Write(sb.ToString());
            w.Close();
        }

    }

}