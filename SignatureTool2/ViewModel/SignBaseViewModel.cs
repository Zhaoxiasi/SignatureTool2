using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SignatureTool2.ViewModel
{
    public class SignBaseViewModel : ViewModelBase
    {

        public ICommand SignCommand { get; set; }
        public ICommand StopCommand { get; set; }
        protected  void OnSign(string company)
        {
            if (company.ToLower().Equals("gemoo")) { }
            else
                Clipboard.SetDataObject("cIn02x0WqfoJ{172");
        }
    }
}
