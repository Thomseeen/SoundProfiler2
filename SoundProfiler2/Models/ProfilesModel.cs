﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using Util.MVVM;

namespace SoundProfiler2.Models {
    public class ProfilesModel : BaseModel {
        #region Constants
        public const string DEFAULT_PROFILES_FILEPATH = "profiles.json";
        #endregion Constants

        #region Private Fields
        private string filePath;

        private ObservableCollection<ProfileModel> profiles;
        #endregion Private Fields

        #region Properties
        public string FilePath {
            get => filePath;
            set { filePath = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProfileModel> Profiles {
            get => profiles;
            set { profiles = value; OnPropertyChanged(); }
        }
        #endregion Properties

        #region Public Methods
        public static ProfilesModel GetDefaultModel() {
            return new ProfilesModel() {
                FilePath = DEFAULT_PROFILES_FILEPATH,
                Profiles = new ObservableCollection<ProfileModel>() {
                    new ProfileModel() {
                        Name = "Focus",
                        CategoryVolumes = new ObservableCollection<CategoryVolumeModel>() {
                            new CategoryVolumeModel() {
                                Name = "Multimedia",
                                Volume = 0f
                            },
                            new CategoryVolumeModel() {
                                Name = "Game",
                                Volume = 1f
                            },
                            new CategoryVolumeModel() {
                                Name = "Communication",
                                Volume = 1f
                            }
                        }
                    },
                    new ProfileModel() {
                        Name = "Casual",
                        CategoryVolumes = new ObservableCollection<CategoryVolumeModel>() {
                            new CategoryVolumeModel() {
                                Name = "Multimedia",
                                Volume = 0.25f
                            },
                            new CategoryVolumeModel() {
                                Name = "Game",
                                Volume = 0.5f
                            },
                            new CategoryVolumeModel() {
                                Name = "Communication",
                                Volume = 1f
                            }
                        }
                    },
                    new ProfileModel() {
                        Name = "Immersion",
                        CategoryVolumes = new ObservableCollection<CategoryVolumeModel>() {
                            new CategoryVolumeModel() {
                                Name = "Multimedia",
                                Volume = 0f
                            },
                            new CategoryVolumeModel() {
                                Name = "Game",
                                Volume = 1f
                            },
                            new CategoryVolumeModel() {
                                Name = "Communication",
                                Volume = 0.25f
                            }
                        }
                    }
                }
            };
        }
        #endregion Public Methods

        #region Base Overrides
        public override int GetHashCode() {
            return FilePath.GetHashCode();
        }

        public override bool Equals(object obj) {
            return obj is ProfilesModel model &&
                   FilePath == model.FilePath;
        }

        public override string ToString() {
            return $"{Path.GetFileName(FilePath)} [{base.ToString()}]";
        }
        #endregion Base Overrides
    }

    public class ProfileModel : BaseModel {
        #region Private Fields
        private string name;

        private ObservableCollection<CategoryVolumeModel> categoryVolumes;
        #endregion Private Fields

        #region Properties
        public string Name {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CategoryVolumeModel> CategoryVolumes {
            get => categoryVolumes;
            set { categoryVolumes = value; OnPropertyChanged(); }
        }
        #endregion Properties

        #region Base Overrides
        public override int GetHashCode() {
            return name.GetHashCode();
        }

        public override bool Equals(object obj) {
            return obj is ProfileModel model &&
                   Name == model.Name;
        }

        public override string ToString() {
            return $"{Name} [{base.ToString()}]";
        }
        #endregion Base Overrides
    }

    public class CategoryVolumeModel : BaseModel {
        #region Private Fields
        private string name;

        private float volume;
        #endregion Private Fields

        #region Properties
        public string Name {
            get => name;
            set { name = value; OnPropertyChanged(); }
        }

        public float Volume {
            get => volume;
            set { volume = value; OnPropertyChanged(); }
        }
        #endregion Properties

        #region Base Overrides
        public override int GetHashCode() {
            return name.GetHashCode() + (int)volume;
        }

        public override bool Equals(object obj) {
            return obj is CategoryVolumeModel model &&
                   Name == model.Name &&
                   Volume == model.Volume;
        }

        public override string ToString() {
            return $"{Name}:{Volume} [{base.ToString()}]";
        }
        #endregion Base Overrides
    }
}
