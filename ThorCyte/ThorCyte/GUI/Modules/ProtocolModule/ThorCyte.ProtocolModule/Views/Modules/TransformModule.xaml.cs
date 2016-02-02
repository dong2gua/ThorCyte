﻿using System.Windows.Controls;
using ThorCyte.ProtocolModule.ViewModels.Modules;

namespace ThorCyte.ProtocolModule.Views.Modules
{
    /// <summary>
    /// Interaction logic for TransformModule.xaml
    /// </summary>
    public partial class TransformModule
    {
        public TransformModule()
        {
            InitializeComponent();
        }

        private int _errorcount = 0;

        private void Tb_OnError(object sender, ValidationErrorEventArgs e)
        {
            //Debug.WriteLine("****************" + ((TextBox)sender).Name + "_OnError*****************");
            //Debug.WriteLine("Event action :"+ e.Action);
            //Debug.WriteLine("Event error content" + e.Error.ErrorContent);

            if (e.Action == ValidationErrorEventAction.Added)
            {
                _errorcount++;
            }
            else
            {
                _errorcount--;
            }
            //Debug.WriteLine("ERROR COUNT = " + _errorcount);

            if (_errorcount != 0)
            {
                ((TransformModVm) DataContext).IsVaild = false;
            }
            else
            {
                ((TransformModVm)DataContext).IsVaild = true; ;
            }
        }
    }
}
