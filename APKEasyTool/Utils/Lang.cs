using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace APKEasyTool
{
    //We use XML for langage which allows users to edit and translate without compiling
    public class Lang
    {
        private static MainForm main;

        #region lang fields
        //Global
        public static string APKTOOL_NOT_SEL_MBOX;
        public static string APK_NOT_SEL_MBOX;

        //Main tab
        public static string SEL_FILES_DIAG;
        public static string INVALID_NOTICE;
        public static string PAK_ID_COPY_NOTICE;
        public static string ACTIVITY_NOT_FOUND;
        public static string SEL_DEC_DIAG;
        public static string NOT_A_DEC_2_NOTICE;
        public static string APK_DEC_NOT_SET_MBOX;
        public static string PLZ_NAME_DEC_MBOX;
        public static string DEC_AGAIN_MBOX;
        public static string APK_DEX_NOT_SEL_MBOX;
        public static string ENTER_NAME_COM_MBOX;
        public static string DEC_APK_FIRST_MBOX;
        public static string COM_APK_FIRST_MBOX;
        public static string ADB_BUSY_MBOX;
        public static string ZIPALIGN_FIRST_MBOX;
        public static string EXT_APK_FIRST_MBOX;
        public static string CANCEL_MBOX;
        public static string CANCEL_ESC;

        //Smali tab
        public static string SEL_DEX_ODEX_DIAG;
        public static string SEL_DIS_DIR_DIAG;
        public static string DEX_NOT_SEL_MBOX;
        public static string DIR_NOT_SET_MBOX;
        public static string PLZ_ENTER_DIS_NAME_MBOX;
        public static string SEL_DIS_FOLDER_DIAG;
        public static string SEL_SMA_DIR_DIAG;
        public static string PLZ_ENTER_SMA_NAME_MBOX;

        //Log tab
        public static string CLEAR_LOGS_MBOX;
        public static string CLEAR_LOGS_MBOX_ERR;
        public static string LOG_DIR_MBOX_ERR;
        public static string PLEASE_READ_FAQ;

        //FW tab
        public static string AND_FW_PACK_LBL;
        public static string SEL_FW_DIAG;
        public static string FW_NOT_SEL_MBOX;
        public static string SEL_FW_FOLDER_DIAG;
        public static string SEL_FW_STORED_DIAG;

        //Options tab
        public static string SEL_DEC_DIR_DIAG;
        public static string SEL_COM_DIR_DIAG;
        public static string SEL_EXT_DIR_DIAG;
        public static string SEL_ZIP_DIR_DIAG;
        public static string SEL_PK8_FILE_DIAG;
        public static string SEL_PEM_FILE_DIAG;
        public static string SEL_AAPT_FILE_DIAG;
        public static string SEL_KEY_FILE_DIAG;
        public static string SEL_ALL_DIR_DIAG;
        public static string HIS_CLEAR_MBOX;
        public static string RES_DIAG;

        //AIF form
        public static string SAVE_FILE_FILTER;

        //U form
        public static string DOWN_STAT_LBL;
        public static string DOWNLOADING_LBL;
        public static string DOWN_OK_LBL;
        #endregion

        #region code
        //Jump list
        public static string OPEN_DEC_APK_DIR;
        public static string OPEN_COM_APK_DIR;
        public static string OPEN_EXT_APK_DIR;
        public static string OPEN_ZIP_APK_DIR;
        public static string OPEN_BAKSMALI_APK_DIR;
        public static string OPEN_SMALI_APK_DIR;

        //CMD
        public static string DEC_APK_FILE_NOTICE;
        public static string DEC_JAR_FILE_NOTICE;
        public static string DEC_SUCCESS_MBOX;
        public static string DEC_FAIL_MBOX;

        public static string COM_APK_FILE_NOTICE;
        public static string COM_JAR_FILE_NOTICE;
        public static string COM_SUCCESS_MBOX;
        public static string COM_FAIL_MBOX;

        public static string KEY_NOT_SEL_MBOX;
        public static string SIGN_APK_FILE_NOTICE;
        public static string SIGN_SUCCESS_MBOX;
        public static string SIGN_FAIL_MBOX;

        public static string INS_APK_FILE_LOG;
        public static string INS_APK_FILE_NOTICE;
        public static string INS_SUCCESS_MBOX;
        public static string INS_FAIL_MBOX;

        public static string CANT_ZIPALIGN_MBOX;
        public static string ZIPALIGN_APK_FILE_NOTICE;
        public static string ZIPALIGN_SUCCESS_MBOX;
        public static string ZIPALIGN_FAIL_MBOX;

        public static string VERI_APK_FILE_LOG;
        public static string ZIPALIGN_OUTPUT_DIS_LOG;
        public static string VERI_APK_FILE_NOTICE;
        public static string VERI_SUCCESS_MBOX;
        public static string VERI_FAIL_MBOX;

        public static string FW_APK_FILE_LOG;
        public static string FW_SUCCESS_MBOX;
        public static string FW_FAIL_MBOX;

        public static string FW_CLR_APK_FILE_LOG;
        public static string FW_CLR_SUCCESS_MBOX;
        public static string FW_CLR_FAIL_MBOX;

        public static string EXT_APK_FILE_LOG;
        public static string EXT_SUCCESS_MBOX;
        public static string EXT_FAIL_MBOX;

        public static string ZIP_APK_FILE_LOG;
        public static string ZIP_SUCCESS_MBOX;
        public static string ZIP_FAIL_MBOX;

        public static string BAKSMALI_LOG;
        public static string BAKSMALI_SUCCESS_MBOX;
        public static string BAKSMALI_FAIL_MBOX;

        public static string SMALI_LOG;
        public static string SMALI_SUCCESS_MBOX;
        public static string SMALI_FAIL_MBOX;

        public static string CANCELLED_LOG;

        //Return Codes
        public static string ADB_APK_INSTALL_FAIL_ERR;
        public static string NO_SUCH_FILE_ERR;
        public static string KEY_PASS_INCORRECT_ERR;
        public static string RENAME_FILE_ERR;
        public static string STRING_INDEX_ERR;
        public static string XML_PARSE_ERR;

        //UI
        public static string RESOURCE_MISSING_NOTICE;
        public static string INSTANCE_TITLE;
        public static string LOG_OUTPUT_BTN;
        public static string JAVA_NOT_INSTALLED_NOTICE;

        public static string READ_APK;
        public static string DONE;
        public static string ERROR_READ_APK;
        public static string CHK_FOR_UPDATE;
        public static string NO_UPDATE;

        #endregion

        public Lang(MainForm Main)
        {
            main = Main;
        }

        public static Dictionary<string, Dictionary<string, string>> _localizations = new Dictionary<string, Dictionary<string, string>>();

        public static string _currentLocalization = "APK Easy Tool";

        public static string Localize(string key)
        {
            try
            {
                if (_localizations[_currentLocalization].ContainsKey(key))
                    return _localizations[_currentLocalization][key];
                else
                    main.LogOutput("Language: " + key + " does not exist");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("E: " + ex.Message + ": " + key + "\n");
                main.LogOutput("E: " + ex.Message + ": " + key);
            }
            return key;
        }

        public static string Localize(string key, string orig)
        {
            try
            {
                //return key;
                if (_localizations[_currentLocalization].ContainsKey(key))
                    return _localizations[_currentLocalization][key];
            }
            catch (Exception ex)
            {
                Debug.WriteLine("E: " + ex.Message + ": " + key + "\n");
                return orig;
                //main.richTextBoxLogs.Text += "E: " + ex.Message + ": " + key + "\n";
            }
            return orig;
        }

        public static bool LoadLocalization(string pFile)
        {
            _localizations.Clear();
            try
            {
                if (File.Exists(pFile) == false)
                {
                    return false;
                }

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load(pFile);
                XmlNodeList nodeList = xmldoc.SelectNodes("Language/Localized");

                _localizations.Add("APK Easy Tool", new Dictionary<string, string>());

                string xmlnode = "";
                try
                {
                    if (nodeList.Count > 0)
                    {
                        foreach (XmlNode node in nodeList)
                        {
                            xmlnode = node.Attributes["name"].Value;
                            // Debug.WriteLine(xmlnode);
                            if (!_localizations[_currentLocalization].ContainsKey(xmlnode))
                                _localizations["APK Easy Tool"].Add(xmlnode, node.Attributes["value"].Value.Replace("[(new-line)]", "\r\n"));
                            else
                                main.LogOutput("Language: " + xmlnode + " exists");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("W: There was an error loading localization .xml file\n" + ex);
                    main.LogOutput("W: There was an error loading localization .xml file\n" + ex.Message + " " + xmlnode);
                }

                LoadStr();
                LoadStrForm();

                return true;
            }
            catch (Exception ex)
            {
                _localizations.Clear();
                Debug.WriteLine(ex);
                return false;
            }
        }

        public static void LoadStr()
        {
            //Non form
            {
                APKTOOL_NOT_SEL_MBOX = Localize("apktool_not_sel_mbox", "Apktool.jar version is not selected. Please go to options and select it.");
                APK_NOT_SEL_MBOX = Localize("apk_not_sel_mbox", "The APK file is not selected. Please select an APK file.");

                //Main
                SEL_FILES_DIAG = Localize("sel_files_diag", "Select APK or JAR file");
                INVALID_NOTICE = Localize("invalid_notice", "File or folder does not exist");
                PAK_ID_COPY_NOTICE = Localize("pak_id_copy_notice", "Package name copied to clipboard.");
                ACTIVITY_NOT_FOUND = Localize("activity_not_found", "Main activity could not be found");
                SEL_DEC_DIAG = Localize("sel_dec_diag", "Select the decompiled folder project to work with");
                NOT_A_DEC_2_NOTICE = Localize("not_a_dec_2_notice", "The folder you selected is not a decompiled folder of APK");
                APK_DEC_NOT_SET_MBOX = Localize("apk_dec_not_set_mbox", "The decompile directory is not set");
                PLZ_NAME_DEC_MBOX = Localize("plz_name_dec_mbox", "Please enter the file name for decompiling.");
                DEC_AGAIN_MBOX = Localize("dec_again_mbox", "Are you sure you want to decompile again?");
                APK_DEX_NOT_SEL_MBOX = Localize("apk_dex_not_sel_mbox", "The APK file or decompile folder is not selected.");
                ENTER_NAME_COM_MBOX = Localize("enter_name_com_mbox", "Please enter the file name for compiling");
                DEC_APK_FIRST_MBOX = Localize("dec_apk_first_mbox", "Decompile the APK file first");
                COM_APK_FIRST_MBOX = Localize("com_apk_first_mbox", "Compile the APK file first.");
                ADB_BUSY_MBOX = Localize("adb_busy_mbox", "ADB is busy. Do you want to restart ADB?");
                ZIPALIGN_FIRST_MBOX = Localize("zipalign_first_mbox", "Zipalign the APK file first");
                EXT_APK_FIRST_MBOX = Localize("ext_apk_first_mbox", "Extract the APK first.");
                CANCEL_MBOX = Localize("cancel_mbox", "Cancelling will kill the process of exe. Are you sure you want to cancel?");
                CANCEL_ESC = Localize("cancel_esc", "(Press ESC to cancel)");

                //Smali tab
                SEL_DEX_ODEX_DIAG = Localize("sel_dex_odex_diag", "Select dex or odex file");
                SEL_DIS_DIR_DIAG = Localize("sel_dis_dir_diag", "Select the folder to store disassembled smali folder");
                DEX_NOT_SEL_MBOX = Localize("dex_not_sel_mbox", "Dex/odex file is not selected.");
                DIR_NOT_SET_MBOX = Localize("dir_not_set_mbox", "Directory is not set.");
                PLZ_ENTER_DIS_NAME_MBOX = Localize("plz_enter_dis_name_mbox", "Please enter the decompiled smali name");
                SEL_DIS_FOLDER_DIAG = Localize("sel_dis_folder_diag", "Select the disassembled smali folder");
                SEL_SMA_DIR_DIAG = Localize("sel_sma_dir_diag", "Select the folder to store assembled smali");
                PLZ_ENTER_SMA_NAME_MBOX = Localize("plz_enter_sma_name_mbox", "Please enter the compiled smali name");

                //Log tab
                LOG_DIR_MBOX_ERR = Localize("log_dir_mbox_err", "Log directory is not found");
                PLEASE_READ_FAQ = Localize("please_read_faq", "Click here for more infomation about Apktool issues");

                //FW Tab
                AND_FW_PACK_LBL = Localize("and_fw_pack_lbl", "Android Framework Package");
                SEL_FW_DIAG = Localize("sel_fw_diag", "Select the Framework APK file");
                FW_NOT_SEL_MBOX = Localize("fw_not_sel_mbox", "The framework is not selected. Please select a framework.");
                SEL_FW_FOLDER_DIAG = Localize("sel_fw_folder_diag", "Select the folder to store framework APK");
                SEL_FW_STORED_DIAG = Localize("sel_fw_stored_diag", "Select the folder where you stored your framework APKs");

                //Option tab
                SEL_DEC_DIR_DIAG = Localize("sel_dec_dir_diag", "Select the folder to store decompiled apk");
                SEL_COM_DIR_DIAG = Localize("sel_com_dir_diag", "Select the folder to store compiled apk");
                SEL_EXT_DIR_DIAG = Localize("sel_ext_dir_diag", "Select the folder to store extracted apk");
                SEL_ZIP_DIR_DIAG = Localize("sel_zip_dir_diag", "Select the folder to store zipped apk");
                SEL_PK8_FILE_DIAG = Localize("sel_pk8_file_diag", "Select the .pk8 key file");
                SEL_PEM_FILE_DIAG = Localize("sel_pem_file_diag", "Select the .pem key file");
                SEL_KEY_FILE_DIAG = Localize("sel_key_file_diag", "Select the .jks or .keystore key file");
                SEL_AAPT_FILE_DIAG = Localize("sel_aapt_file_diag", "Select the aapt executeable file");
                SEL_ALL_DIR_DIAG = Localize("sel_all_dir_diag", "Select the folder to setup directories to work");
                HIS_CLEAR_MBOX = Localize("his_clear_mbox", "History cleared");
                RES_DIAG = Localize("res_diag", "Are you sure you want to reset to defaults?");

                //AIF
                SAVE_FILE_FILTER = Localize("save_file_filter", "Text file");

                //Update
                DOWNLOADING_LBL = Localize("downloading_lbl", "Downloading...");
                DOWN_OK_LBL = Localize("down_ok_lbl", "Downloaded.");

                //Other
                //Jump list                                                
                OPEN_DEC_APK_DIR = Localize("open_dec_apk_dir", "Open decompiled APK directory");
                OPEN_COM_APK_DIR = Localize("open_com_apk_dir", "Open compiled APK directory");
                OPEN_EXT_APK_DIR = Localize("open_ext_apk_dir", "Open extracted APK directory");
                OPEN_ZIP_APK_DIR = Localize("open_zip_apk_dir", "Open zipped APK directory");
                OPEN_BAKSMALI_APK_DIR = Localize("open_baksmali_apk_dir", "Open baksmali directory");
                OPEN_SMALI_APK_DIR = Localize("open_smali_apk_dir", "Open smali directory");

                //CMD                                                      
                DEC_APK_FILE_NOTICE = Localize("dec_apk_file_notice", "Decompiling APK file...");
                DEC_JAR_FILE_NOTICE = Localize("dec_jar_file_notice", "Decompiling JAR file...");
                DEC_SUCCESS_MBOX = Localize("dec_success_mbox", "Decompile successful.");
                DEC_FAIL_MBOX = Localize("dec_fail_mbox", "Decompile failed. Please read the log");

                COM_APK_FILE_NOTICE = Localize("com_apk_file_notice", "Compiling APK file...");
                COM_JAR_FILE_NOTICE = Localize("com_jar_file_notice", "Compiling JAR file...");
                COM_SUCCESS_MBOX = Localize("com_success_mbox", "Compile successful.");
                COM_FAIL_MBOX = Localize("com_fail_mbox", "Compile failed. Please read the log");

                KEY_NOT_SEL_MBOX = Localize("key_not_sel_mbox", "Keystore file is not selected");
                SIGN_APK_FILE_NOTICE = Localize("sign_apk_file_notice", "Signing APK file...");
                SIGN_SUCCESS_MBOX = Localize("sign_success_mbox", "Sign successful.");
                SIGN_FAIL_MBOX = Localize("sign_fail_mbox", "Sign failed. Please read the log");

                INS_APK_FILE_LOG = Localize("ins_apk_file_log", "Installing APK to device...");
                INS_APK_FILE_NOTICE = Localize("ins_apk_file_notice", "Installing APK to device... Press cancel if it gets stuck");
                INS_SUCCESS_MBOX = Localize("ins_success_mbox", "APK Installed successful");
                INS_FAIL_MBOX = Localize("ins_fail_mbox", "APK installation failed. Please read the log");

                CANT_ZIPALIGN_MBOX = Localize("cant_zipalign_mbox", "Can't zipalign the APK file. Rename or delete the file and try again.");
                ZIPALIGN_APK_FILE_NOTICE = Localize("zipalign_apk_file_notice", "Zipaligning...");
                ZIPALIGN_SUCCESS_MBOX = Localize("zipalign_success_mbox", "Zipalign successful");
                ZIPALIGN_FAIL_MBOX = Localize("zipalign_fail_mbox", "Zipalign failed. Please read the log");

                VERI_APK_FILE_LOG = Localize("veri_apk_file_log", "Verifying alignment of");
                ZIPALIGN_OUTPUT_DIS_LOG = Localize("zipalign_output_dis_log", "Zipalign verbose output is disabled");
                VERI_APK_FILE_NOTICE = Localize("veri_apk_file_notice", "Verifying alignment...");
                VERI_SUCCESS_MBOX = Localize("veri_success_mbox", "Verifying successful");
                VERI_FAIL_MBOX = Localize("veri_fail_mbox", "Verifying failed. Please READ the log");

                FW_APK_FILE_LOG = Localize("fw_apk_file_log", "Installing framework...");
                FW_SUCCESS_MBOX = Localize("fw_success_mbox", "Framework Installed successful");
                FW_FAIL_MBOX = Localize("fw_fail_mbox", "Framework installation failed. Please read the log");

                FW_CLR_APK_FILE_LOG = Localize("fw_clr_apk_file_log", "Clearing framework cache...");
                FW_CLR_SUCCESS_MBOX = Localize("fw_clr_success_mbox", "Framework cache cleared");
                FW_CLR_FAIL_MBOX = Localize("fw_clr_fail_mbox", "Framework cache clearing failed");

                EXT_APK_FILE_LOG = Localize("ext_apk_file_log", "Extracting APK file...");
                EXT_SUCCESS_MBOX = Localize("ext_success_mbox", "Extracting APK successful");
                EXT_FAIL_MBOX = Localize("ext_fail_mbox", "Extracting APK failed. Please read the log");

                ZIP_APK_FILE_LOG = Localize("zip_apk_file_log", "Zipping back to APK file...");
                ZIP_SUCCESS_MBOX = Localize("zip_success_mbox", "Zipping successful");
                ZIP_FAIL_MBOX = Localize("zip_fail_mbox", "Zipping failed. Please read the log");

                BAKSMALI_LOG = Localize("baksmali_file_log", "Baksmaling...");
                BAKSMALI_SUCCESS_MBOX = Localize("baksmali_success_mbox", "Baksmaling successful");
                BAKSMALI_FAIL_MBOX = Localize("baksmali_fail_mbox", "Baksmaling failed. Please read the log");

                SMALI_LOG = Localize("smali_file_log", "Smaling...");
                SMALI_SUCCESS_MBOX = Localize("smali_success_mbox", "Smaling successful");
                SMALI_FAIL_MBOX = Localize("smali_fail_mbox", "Smaling failed. Please read the log");

                CANCELLED_LOG = Localize("cancelled_log", "Cancelled");

                //Return Codes                                             
                ADB_APK_INSTALL_FAIL_ERR = Localize("adb_apk_install_fail_err", "APK installation failed:");
                NO_SUCH_FILE_ERR = Localize("no_such_file_err", "No such file or directory");
                KEY_PASS_INCORRECT_ERR = Localize("key_pass_incorrect_err", "keystore password was incorrect");
                RENAME_FILE_ERR = Localize("rename_file_err", "Unable to rename temporary file");
                STRING_INDEX_ERR = Localize("string_index_err", "String index out of bounds");
                XML_PARSE_ERR = Localize("xml_parse_err", "Error parsing XML: not well-formed (invalid token)");

                //UI                                                       
                RESOURCE_MISSING_NOTICE = Localize("resource_missing_notice", "Apktool or Resources folder are missing. Make sure they are placed beside apkeasytool.exe");
                INSTANCE_TITLE = Localize("instance_title", "Instance");
                LOG_OUTPUT_BTN = Localize("log_output_tab", "Log output");
                JAVA_NOT_INSTALLED_NOTICE = Localize("java_not_installed_notice", "Java is not installed. Please install Java SE or select path to Java executeable on options");

                READ_APK = Localize("read_apk", "Reading APK...");
                DONE = Localize("done", "Done");
                ERROR_READ_APK = Localize("error_read_apk", "There was an error reading APK file");
                CHK_FOR_UPDATE = Localize("chk_for_update", "Checking for update...");
                NO_UPDATE = Localize("no_update", "No update available");
            }
        }

        public static void LoadStrForm()
        {
            //Main Tab
            {
                main.label11.Text = Localize("apktool_ver_lbl", main.label11.Text);
                main.viewLogSidedBtn.Text = Localize("log_output_tab", main.viewLogSidedBtn.Text);

                //Main Tab
                main.selectDecDir.Text = Localize("sel_dec_folder", main.selectDecDir.Text);
                main.toolTip1.SetToolTip(main.selectDecDir, Localize("sel_dec_folder_tip", main.toolTip1.GetToolTip(main.selectDecDir)));
                main.tMain.TabPages[0].Text = Localize("tab_name_main", main.tMain.TabPages[0].Text);
                main.labelMain1.Text = Localize("file_folder_lbl", main.labelMain1.Text);
                main.decLbl.Text = Localize("decompile_name_lbl", main.decLbl.Text);
                main.comLbl.Text = Localize("compile_name_lbl", main.comLbl.Text);
                main.label95.Text = Localize("tip_drop_apk_lbl", main.label95.Text);
                main.label96.Text = Localize("decode_api_lvl_lbl", main.label96.Text);
                main.toolTip1.SetToolTip(main.decodeApiLvl, Localize("api_tip", main.toolTip1.GetToolTip(main.decodeApiLvl)));
                main.adad.Text = Localize("rebuild_api_lvl_lbl");
                main.toolTip1.SetToolTip(main.rebuildApiLvl, Localize("api_tip", main.toolTip1.GetToolTip(main.rebuildApiLvl)));

                //Buttons
                main.selectApk.Text = Localize("select_btn", main.selectApk.Text);
                main.decApkBtn.Text = Localize("decompile", main.decApkBtn.Text);
                main.comApkBtn.Text = Localize("compile", main.comApkBtn.Text);
                main.signApkBtn.Text = Localize("sign_apk_btn", main.signApkBtn.Text);
                main.zipAlignBtn.Text = Localize("zipalign__btn", main.zipAlignBtn.Text);
                main.extractApkBtn.Text = Localize("extract_apk_btn", main.extractApkBtn.Text);
                main.zipApkBtn.Text = Localize("zip_apk_btn", main.zipApkBtn.Text);
                main.installApkBtn.Text = Localize("install_apk_btn", main.installApkBtn.Text);
                main.chkAliBtn.Text = Localize("check_align_btn", main.chkAliBtn.Text);
                main.openDecOutput.Text = Localize("dec_apk_dir_btn", main.openDecOutput.Text);
                main.openComOutput.Text = Localize("com_apk_dir_btn", main.openComOutput.Text);
                main.openExt.Text = Localize("ext_apk_dir_btn", main.openExt.Text);
                main.openZipApk.Text = Localize("zip_apk_dir_btn", main.openZipApk.Text);

                //Quick Options
                main.useTagChkBox.Text = Localize("fw_use_tag_lbl", main.useTagChkBox.Text);
                main.toolTip1.SetToolTip(main.tagTxtBox, Localize("fw_use_tag_tip", main.toolTip1.GetToolTip(main.tagTxtBox)));
                main.decFcheckBox.Text = Localize("dec_force_del_dir_chk", main.decFcheckBox.Text);
                main.toolTip1.SetToolTip(main.decFcheckBox, Localize("dec_force_del_dir_tip", main.toolTip1.GetToolTip(main.decFcheckBox)));
                main.comFcheckBox.Text = Localize("com_skip_changes_chk", main.comFcheckBox.Text);
                main.toolTip1.SetToolTip(main.comFcheckBox, Localize("com_skip_changes_tip", main.toolTip1.GetToolTip(main.comFcheckBox)));
                main.comCcheckBox.Text = Localize("com_keep_orig_sig_chk", main.comCcheckBox.Text);
                main.toolTip1.SetToolTip(main.comCcheckBox, Localize("com_keep_orig_sig_tip", main.toolTip1.GetToolTip(main.comCcheckBox)));
                main.chkBoxUseAapt2.Text = Localize("com_use_appt2_chk", main.chkBoxUseAapt2.Text);
                main.toolTip1.SetToolTip(main.chkBoxUseAapt2, Localize("com_use_appt2_tip", main.toolTip1.GetToolTip(main.chkBoxUseAapt2)));
                main.signApkCheckBox.Text = Localize("sign_apk_after_com_chk", main.signApkCheckBox.Text);
                main.installApkChkBox.Text = Localize("sign_install_apk_after_sign_chk", main.installApkChkBox.Text);
                main.overApkChecked.Text = Localize("sign_overwrite_apk_chk", main.overApkChecked.Text);

                //Info
                main.label50.Text = Localize("package_name_lbl", main.label50.Text);
                main.label61.Text = Localize("launch_activity_lbl", main.label61.Text);
                main.label53.Text = Localize("min_sdk_ver_lbl", main.label53.Text);
                main.label54.Text = Localize("target_sdk_ver_lbl", main.label54.Text);
                main.label51.Text = Localize("version_name_lbl", main.label51.Text);
                main.label52.Text = Localize("version_code_lbl", main.label52.Text);
                main.label97.Text = Localize("signature_scheme_lbl", main.label97.Text);
                main.toolTip1.SetToolTip(main.psPicBox, Localize("ps_link", main.toolTip1.GetToolTip(main.psPicBox)));
                main.toolTip1.SetToolTip(main.acPicBox, Localize("apkcombo_link", main.toolTip1.GetToolTip(main.acPicBox)));
                main.toolTip1.SetToolTip(main.apPicBox, Localize("apkpure_link", main.toolTip1.GetToolTip(main.apPicBox)));
                main.fullApkInfoBtn.Text = Localize("full_apk_info_btn", main.fullApkInfoBtn.Text);
            }

            //Smali tab
            {
                main.tMain.TabPages[1].Text = Localize("smali_bak_tab", main.tMain.TabPages[1].Text);
                main.label75.Text = Localize("baksmali_lbl", main.label75.Text);
                main.label23.Text = Localize("dex_files_lbl", main.label23.Text);
                main.label70.Text = Localize("bak_out_dir_lbl", main.label70.Text);
                main.label89.Text = Localize("name_dec_smali_lbl", main.label89.Text);
                main.deodexChkBox.Text = Localize("deodex_lbl", main.deodexChkBox.Text);
                main.label87.Text = Localize("smali_lbl", main.label87.Text);
                main.label69.Text = Localize("dis_dir_lbl", main.label69.Text);
                main.label71.Text = Localize("smali_out_dir_lbl", main.label71.Text);
                main.label90.Text = Localize("name_of_com_smali_lbl", main.label90.Text);
                main.smaliDisBtn.Text = Localize("dec_smali_btn", main.smaliDisBtn.Text);
                main.smaliAssBtn.Text = Localize("com_smali_btn", main.smaliAssBtn.Text);
                main.baksSelFileBtn.Text = Localize("select_btn", main.baksSelFileBtn.Text);
                main.smaliSelFileBtn.Text = Localize("select_btn", main.smaliSelFileBtn.Text);
                main.baksChangeBtn.Text = Localize("change_btn", main.baksChangeBtn.Text);
                main.smaliChangeBtn.Text = Localize("change_btn", main.smaliChangeBtn.Text);
            }

            //Log tab
            {
                main.tMain.TabPages[2].Text = Localize("log_output_tab", main.tMain.TabPages[2].Text);
                main.clearLogBtn.Text = Localize("clr_this_log_btn", main.clearLogBtn.Text);
                main.saveLogBtn.Text = Localize("save_log_btn", main.saveLogBtn.Text);
            }

            //FW tab
            {
                main.tMain.TabPages[3].Text = Localize("framework", main.tMain.TabPages[3].Text);
                main.label83.Text = Localize("framework", main.label83.Text);
                main.label7.Text = Localize("framework_file_lbl", main.label7.Text);
                main.label84.Text = Localize("install_dir_lbl", main.label84.Text);
                main.tagFwChkBox.Text = Localize("tag_fw_lbl", main.tagFwChkBox.Text);
                main.label85.Text = Localize("fw_dir_lbl", main.label85.Text);
                main.label86.Text = Localize("fw_dir_note_lbl", main.label86.Text);
                main.label5.Text = Localize("fw_dir_note2_lbl", main.label5.Text);
                main.label14.Text = Localize("fw_dir_note3_lbl", main.label14.Text);
                main.selFramework.Text = Localize("sel_fw_btn", main.selFramework.Text);
                main.installFwBtn.Text = Localize("install_fw_btn", main.installFwBtn.Text);
                main.openFwDirBtn.Text = Localize("open_fw_dir_btn", main.openFwDirBtn.Text);
                main.clrFwCacheBtn.Text = Localize("clr_fw_btn", main.clrFwCacheBtn.Text);
                main.changeInsFwTxtbox.Text = Localize("change_btn", main.changeInsFwTxtbox.Text);
                main.changeDirFwTxtbox.Text = Localize("change_btn", main.changeDirFwTxtbox.Text);
            }

            //Options 1 tab
            {
                main.tMain.TabPages[4].Text = Localize("options_tab", main.tMain.TabPages[4].Text);
                main.oTab.TabPages[0].Text = Localize("general", main.oTab.TabPages[0].Text);
                main.label77.Text = Localize("general", main.label77.Text);

                //General
                main.chkUpdateChkBox.Text = Localize("chk_update_chk", main.chkUpdateChkBox.Text);
                main.winPosCheckBox.Text = Localize("rem_win_pos_chk", main.winPosCheckBox.Text);
                main.toolTip1.SetToolTip(main.winPosCheckBox, Localize("rem_win_pos_tip", main.toolTip1.GetToolTip(main.winPosCheckBox)));
                main.disHisBox.Text = Localize("dis_dropdown_his_chk", main.disHisBox.Text);
                main.clrHisBtn.Text = Localize("clr_his_btn", main.clrHisBtn.Text);
                main.label6.Text = Localize("con_menu_lbl", main.label6.Text);
                main.insBtn.Text = Localize("ins_lbl", main.insBtn.Text);
                main.uninsBtn.Text = Localize("unins_lbl", main.uninsBtn.Text);
                main.taskBarCheckBox.Text = Localize("task_bar_chk", main.taskBarCheckBox.Text);
                main.restartBtn.Text = Localize("restart_btn", main.restartBtn.Text);
                main.tmpFolBtn.Text = Localize("temp_folder_btn", main.tmpFolBtn.Text);
                main.label2.Text = Localize("lang_lbl", main.label2.Text);
                main.label21.Text = Localize("apktool_ver_lbl", main.label21.Text);
                main.toolTip1.SetToolTip(main.apkToolComboBox, Localize("apktool_ver_tip", main.toolTip1.GetToolTip(main.apkToolComboBox)));
                //main.label92.Text = Localize("cmd_mode_lbl", main.label92.Text);
                // main.toolTip1.SetToolTip(main.cmdModeChkBox, Localize("cmd_mode_tip", main.toolTip1.GetToolTip(main.winPosCheckBox)));
                main.useExtJava.Text = Localize("use_java_por_lbl", main.useExtJava.Text);
                main.label40.Text = Localize("mb_lbl", main.label40.Text);
                main.label62.Text = Localize("comp_lvl_lbl", main.label62.Text);
                main.useJaxaXmxChkBox.Text = Localize("java_heap_lbl", main.useJaxaXmxChkBox.Text);
                main.toolTip1.SetToolTip(main.javaHeapSizeNum, Localize("java_heap_tip", main.toolTip1.GetToolTip(main.javaHeapSizeNum)));

                //Directory
                main.label78.Text = Localize("dir_lbl", main.label78.Text);
                main.label1.Text = Localize("dec_dir_lbl", main.label1.Text);
                main.label3.Text = Localize("com_dir_lbl", main.label3.Text);
                main.label49.Text = Localize("ext_dir_lbl", main.label49.Text);
                main.label60.Text = Localize("zip_dir_lbl", main.label60.Text);
                main.setupDirBtn.Text = Localize("setup_all_dir_btn", main.setupDirBtn.Text);
                main.changeDecDir.Text = Localize("change_btn", main.changeDecDir.Text);
                main.changeComDir.Text = Localize("change_btn", main.changeComDir.Text);
                main.changeExtDir.Text = Localize("change_btn", main.changeExtDir.Text);
                main.changeZipDir.Text = Localize("change_btn", main.changeZipDir.Text);

                //Other
                main.label37.Text = Localize("other_lbl", main.label37.Text);
                main.resBtn.Text = Localize("reset_btn", main.resBtn.Text);
                main.label103.Text = Localize("reset_note_lbl", main.label103.Text);
            }

            //Options 2 tab
            {
                //Decompile
                main.label63.Text = Localize("decompile", main.label63.Text);
                main.decBcheckBox.Text = Localize("dont_write_info_lbl", main.decBcheckBox.Text);
                main.toolTip1.SetToolTip(main.decBcheckBox, Localize("dont_write_info_tip", main.toolTip1.GetToolTip(main.decBcheckBox)));
                main.decRcheckBox.Text = Localize("dont_dec_res_lbl", main.decRcheckBox.Text);
                main.toolTip1.SetToolTip(main.decRcheckBox, Localize("dont_dec_res_tip", main.toolTip1.GetToolTip(main.decRcheckBox)));
                main.decScheckBox.Text = Localize("dont_dec_dex_lbl", main.decScheckBox.Text);
                main.toolTip1.SetToolTip(main.decScheckBox, Localize("dont_dec_dex_tip", main.toolTip1.GetToolTip(main.decScheckBox)));
                main.decNoAssetsCheckBox.Text = Localize("dont_copy_unk_asset_lbl", main.decNoAssetsCheckBox.Text);
                main.decMcheckBox.Text = Localize("keep_file_orig_lbl");
                main.toolTip1.SetToolTip(main.decMcheckBox, Localize("keep_file_orig_tip", main.toolTip1.GetToolTip(main.decMcheckBox)));
                main.decKcheckBox.Text = Localize("keep_bro_res_lbl", main.decKcheckBox.Text);
                main.toolTip1.SetToolTip(main.decKcheckBox, Localize("keep_bro_res_tip", main.toolTip1.GetToolTip(main.decKcheckBox)));
                main.decFcheckBox.Text = Localize("dec_force_del_dir_chk", main.decFcheckBox.Text);
                main.toolTip1.SetToolTip(main.decFcheckBox, Localize("dec_force_del_dir_tip", main.toolTip1.GetToolTip(main.decFcheckBox)));
                main.decForceManifestChkBox.Text = Localize("force_manifest_chkbox", main.decForceManifestChkBox.Text);
                main.toolTip1.SetToolTip(main.decForceManifestChkBox, Localize("force_manifest_tip", main.toolTip1.GetToolTip(main.decForceManifestChkBox)));
                main.decNoAssetsCheckBox.Text = Localize("dont_copy_unk_asset_lbl", main.decNoAssetsCheckBox.Text);
                main.decOnlyMainClassesChkBox.Text = Localize("only_dis_main_dex", main.decOnlyMainClassesChkBox.Text);

                //Compile
                main.label76.Text = Localize("compile", main.label76.Text);
                main.comDcheckBox.Text = Localize("set_apk_debug_lbl", main.comDcheckBox.Text);
                main.toolTip1.SetToolTip(main.comDcheckBox, Localize("set_apk_debug_tip", main.toolTip1.GetToolTip(main.comDcheckBox)));
                main.comCcheckBox.Text = Localize("com_keep_orig_sig_chk", main.comCcheckBox.Text);
                main.toolTip1.SetToolTip(main.comCcheckBox, Localize("com_keep_orig_sig_tip", main.toolTip1.GetToolTip(main.comCcheckBox)));
                main.comNCcheckBox.Text = Localize("dis_cru_res_lbl", main.comNCcheckBox.Text);
                main.comFcheckBox.Text = Localize("com_skip_changes_chk", main.comFcheckBox.Text);
                main.toolTip1.SetToolTip(main.comFcheckBox, Localize("com_skip_changes_tip", main.toolTip1.GetToolTip(main.comFcheckBox)));
                main.chkBoxUseAapt2.Text = Localize("use_aapt2_lbl", main.chkBoxUseAapt2.Text);
                main.useAaptPathChkBox.Text = Localize("use_aapt_path", main.useAaptPathChkBox.Text);
                main.setAaptPathBtn.Text = Localize("change_btn", main.setAaptPathBtn.Text);
            }

            //Options 3 tab
            {
                //Signer
                main.label80.Text = Localize("signer", main.label80.Text);
                main.oTab.TabPages[2].Text = Localize("signer", main.oTab.TabPages[2].Text);
                main.signApkCheckBox.Text = Localize("sign_apk_after_com_chk", main.signApkCheckBox.Text);
                main.installApkChkBox.Text = Localize("ins_apk_sign_lbl", main.installApkChkBox.Text);
                main.overApkChecked.Text = Localize("over_apk_lbl", main.overApkChecked.Text);
                main.useJksCheckBox.Text = Localize("use_key_lbl", main.useJksCheckBox.Text);
                main.label73.Text = Localize("key_pw_lbl", main.label73.Text);
                main.label72.Text = Localize("pri_keys_lbl", main.label72.Text);
                main.label10.Text = Localize("pk8_lbl", main.label10.Text);
                main.label56.Text = Localize("pem_lbl", main.label56.Text);
                main.selJksTxtBox.Text = Localize("change_btn", main.selJksTxtBox.Text);
                main.selPk8TxtBox.Text = Localize("change_btn", main.selPk8TxtBox.Text);
                main.selPemTxtBox.Text = Localize("change_btn", main.selPemTxtBox.Text);
                main.label30.Text = Localize("signing_note", main.label30.Text);

                string default_str = Localize("default_str", "Default");
                string default_false_strstr = Localize("false_str", "False");
                string true_str = Localize("true_str", "True");
                string only_str = Localize("only_str", "Only");

                int v2index = main.v2signComboBox.SelectedIndex;
                main.v2signComboBox.Items.Clear();
                main.v2signComboBox.Items.Add(default_str);
                main.v2signComboBox.Items.Add(true_str);
                main.v2signComboBox.Items.Add(default_false_strstr);
                main.v2signComboBox.SelectedIndex = v2index;

                int v3index = main.v3signComboBox.SelectedIndex;
                main.v3signComboBox.Items.Clear();
                main.v3signComboBox.Items.Add(default_str);
                main.v3signComboBox.Items.Add(true_str);
                main.v3signComboBox.Items.Add(default_false_strstr);
                main.v3signComboBox.SelectedIndex = v3index;

                int v4index = main.v4signComboBox.SelectedIndex;
                main.v4signComboBox.Items.Clear();
                main.v4signComboBox.Items.Add(default_str);
                main.v4signComboBox.Items.Add(true_str);
                main.v4signComboBox.Items.Add(default_false_strstr);
                main.v4signComboBox.Items.Add(only_str);
                main.v4signComboBox.SelectedIndex = v4index;

                main.v2SignLbl.Text = Localize("v2_signing_enabled_lbl", main.v2SignLbl.Text);
                main.v3SignLbl.Text = Localize("v3_signing_enabled_lbl", main.v3SignLbl.Text);
                main.v4SignLbl.Text = Localize("v4_signing_enabled_lbl", main.v4SignLbl.Text);
            }

            //Options 4 tab
            {
                //Zipalign
                main.zipPcheckBox.Text = Localize("page_align_lbl", main.zipPcheckBox.Text);
                main.zipVcheckBox.Text = Localize("verbose_lbl", main.zipVcheckBox.Text);
                main.zipAfterSignChkBox.Text = Localize("zip_after_com_lbl", main.zipAfterSignChkBox.Text);
                main.zipFcheckBox.Text = Localize("over_exist_zip_lbl", main.zipFcheckBox.Text);
                main.zipZcheckBox.Text = Localize("recom_zopfil_lbl", main.zipZcheckBox.Text);
            }

            //About
            {
                main.tMain.TabPages[5].Text = Localize("about_tab", main.tMain.TabPages[5].Text);
                main.label9.Text = Localize("about_tab", main.label9.Text);
                main.label20.Text = Localize("software_lbl", main.label20.Text);
                main.label16.Text = Localize("credits_lbl", main.label16.Text);
                main.label55.Text = Localize("softpedia_lbl", main.label55.Text);
                main.linkSoft.Text = Localize("softpedia_link_lbl", main.linkSoft.Text);
                main.label33.Text = Localize("contact_lbl", main.label33.Text);
                main.label67.Text = Localize("disclaimer_lbl", main.label67.Text);
                main.label4.Text = Localize("disnote_lbl", main.label4.Text);
                main.label18.Text = Localize("translated_by_lbl", main.label18.Text);
                main.label28.Text = Localize("translation_credit_lbl", main.label28.Text);
            }
        }
    }
}
