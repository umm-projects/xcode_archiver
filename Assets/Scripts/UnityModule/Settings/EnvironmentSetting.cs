using UnityEngine;

namespace UnityModule.Settings {

    /// <summary>
    /// 環境設定を拡張
    /// </summary>
    // ReSharper disable once PartialTypeWithSinglePart
    // ReSharper disable once RedundantExtendsListEntry
    public partial class EnvironmentSetting : Setting<EnvironmentSetting> {

        /// <summary>
        /// Xcode Archive を起動するかどうかの実体
        /// </summary>
        [SerializeField]
        private bool shouldRunXcodeArchive = true;

        /// <summary>
        /// Xcode Archive を起動するかどうか
        /// </summary>
        public bool ShouldRunXcodeArchive {
            get {
                return this.shouldRunXcodeArchive;
            }
            set {
                this.shouldRunXcodeArchive = value;
            }
        }

        /// <summary>
        /// .xcworkspace を利用しているかどうかの実体
        /// </summary>
        [SerializeField]
        private bool useXCWorkspace = true;

        /// <summary>
        /// .xcworkspace を利用しているかどうか
        /// </summary>
        public bool UseXCWorkspace {
            get {
                return this.useXCWorkspace;
            }
            set {
                this.useXCWorkspace = value;
            }
        }

    }

}