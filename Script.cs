using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    public class Script
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext scriptcontext)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (scriptcontext.ExternalPlanSetup == null)
            {
                MessageBox.Show("No plan is open.", "Error");
                return;
            }

            int i = 0;
            foreach (var beam in scriptcontext.ExternalPlanSetup.Beams)
            {
                if (!beam.IsSetupField & beam.MLC != null & beam.MLCPlanType != MLCPlanType.Static)
                {
                    ++i;
                }
            }
            if (i < 1)
            {
                MessageBox.Show("Plan does not contain dynamic fields.", "Error");
                return;
            }

            scriptcontext.Patient.BeginModifications();

            FieldEditor.MainWindow mainWindow = new FieldEditor.MainWindow(scriptcontext);
            mainWindow.ShowDialog();
        }
    }
}
