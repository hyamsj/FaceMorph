using FaceMorphMVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FaceMorphMVVM.Commands
{
    public class ImageDetailsUpdateCommand : ICommand
    {
        private ImageDetailsViewModel _viewModel;
        Action<object> executeMethod;
        Func<object, bool> canexecuteMethod;

        /// <summary>
        /// Initializes a new instance of the ImageDetailsUpdateCommand class
        /// </summary>
        public ImageDetailsUpdateCommand(Action<object> executeMethod, Func<object, bool> canexecuteMethod,ImageDetailsViewModel viewModel)
        {
            this.executeMethod = executeMethod;
            this.canexecuteMethod = canexecuteMethod;
            _viewModel = viewModel;
            
        }


        #region ICommand Members
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return canexecuteMethod(parameter);
            //return string.IsNullOrWhiteSpace(_viewModel.ImageDetails.Error);
            
        }

        public void Execute(object parameter)
        {
            executeMethod(parameter);
            _viewModel.SaveChanges();
        }
        #endregion
    }
}
