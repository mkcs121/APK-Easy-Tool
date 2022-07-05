using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;

namespace APKEasyTool
{
    public class TaskBarJumpList
    {
        private JumpList list;

        /// <summary>
        /// Creating a JumpList for the application
        /// </summary>
        /// <param name="windowHandle"></param>
        public TaskBarJumpList(IntPtr windowHandle)
        {
            list = JumpList.CreateJumpListForIndividualWindow(TaskbarManager.Instance.ApplicationId, windowHandle);
            list.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;
            BuildList();
        }

        public void AddToRecent(string destination)
        {
            //Call JumpList.AddToRecent(destination); because of bug
            JumpList.AddToRecent(destination);
            list.Refresh();
        }

        /// <summary>
        /// Builds the Jumplist
        /// </summary>
        private void BuildList()
        {
            try
            {
                JumpListCustomCategory userActionsCategory = new JumpListCustomCategory("Actions");
                //JumpListLink userActionLink = new JumpListLink(Assembly.GetEntryAssembly().Location, "Clear History");
                //userActionLink.Arguments = "1";

                JumpListLink jlD = new JumpListLink(Assembly.GetEntryAssembly().Location, Lang.OPEN_DEC_APK_DIR);
                jlD.IconReference = new IconReference(Assembly.GetEntryAssembly().Location, 3);
                jlD.Arguments = "DecDir";

                JumpListLink jlC = new JumpListLink(Assembly.GetEntryAssembly().Location, Lang.OPEN_COM_APK_DIR);
                jlC.IconReference = new IconReference(Assembly.GetEntryAssembly().Location, 2);
                jlC.Arguments = "ComDir";

                JumpListLink jlE = new JumpListLink(Assembly.GetEntryAssembly().Location, Lang.OPEN_EXT_APK_DIR);
                jlE.IconReference = new IconReference(Assembly.GetEntryAssembly().Location, 1);
                jlE.Arguments = "ExtDir";

                JumpListLink jlZ = new JumpListLink(Assembly.GetEntryAssembly().Location, Lang.OPEN_ZIP_APK_DIR);
                jlZ.IconReference = new IconReference(Assembly.GetEntryAssembly().Location, 1);
                jlZ.Arguments = "ZipDir";

                JumpListLink jlB = new JumpListLink(Assembly.GetEntryAssembly().Location, Lang.OPEN_BAKSMALI_APK_DIR);
                jlB.IconReference = new IconReference(Assembly.GetEntryAssembly().Location, 4);
                jlB.Arguments = "BakDir";

                JumpListLink jlS = new JumpListLink(Assembly.GetEntryAssembly().Location, Lang.OPEN_SMALI_APK_DIR);
                jlS.IconReference = new IconReference(Assembly.GetEntryAssembly().Location, 4);
                jlS.Arguments = "SmaDir";

                list.AddUserTasks(jlD);
                list.AddUserTasks(jlC);
                list.AddUserTasks(jlE);
                list.AddUserTasks(jlZ);
                list.AddUserTasks(jlB);
                list.AddUserTasks(jlS);
                //list.AddUserTasks(new JumpListSeparator());

                list.Refresh();
            }
            catch
            {

            }
        }
    }
}
