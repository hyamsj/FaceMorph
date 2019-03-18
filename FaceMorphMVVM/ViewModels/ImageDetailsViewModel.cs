using FaceMorphMVVM.Commands;
using FaceMorphMVVM.Models;
using FaceMorphMVVM.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FaceMorphMVVM.ViewModels
{
    public class ImageDetailsViewModel
    {
        private ImageDetails _imageDetails;
        private ImagePreviewViewModel _childViewModel;
        public ObservableCollection<ImageDetails> images = new ObservableCollection<ImageDetails>();

        /// <summary>
        /// Initialzes a new instance of the ImageDetails class
        /// </summary>
        public ImageDetailsViewModel()
        {
            images.Add(new ImageDetails()
            {
                Title = "Hello"
            });
            images.Add(new ImageDetails()
            {
                Title = "Goodbye"
            });
            _imageDetails = new ImageDetails();
            _imageDetails.Title = "sup";

            _childViewModel = new ImagePreviewViewModel();
            UpdateCommand = new ImageDetailsUpdateCommand(ExecuteMethod, CanExecuteMethod, this);
        }

        private bool CanExecuteMethod(object parameter)
        {
            return string.IsNullOrWhiteSpace(this.ImageDetails.Error);
            
        }

        private void ExecuteMethod(object parameter)
        {
            MessageBox.Show("Hello");
        }
       

        /// <summary>
        /// Gets the ImageDetails instance
        /// </summary>
        public ImageDetails ImageDetails
        {
            get
            {
                return _imageDetails;
            }
        }

        /// <summary>
        /// Gets the UpdateCommand for the ViewModel
        /// </summary>
        public ICommand UpdateCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Saves changes made to the ImageDetails instance
        /// </summary>
        public void SaveChanges()
        {
            ImagePreviewView view = new ImagePreviewView();
            view.DataContext = _childViewModel;
            _childViewModel.Info = ImageDetails.Title + " was updated";
            view.ShowDialog();
        }


    }
}
