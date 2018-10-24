using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace HwandazaWebService.Utils
{
    public sealed class MediaLibrary
    {
        public Task<List<MediaFile>> GetMediaSongs()
        {
            return GetSongs();
        }

        public Task<List<MediaFile>> GetMediaImges()
        {
            return GetImages();
        }

        public Task<List<MediaFile>> GetMediaVideos()
        {
            return GetVideos();
        }
           
        private async Task<List<MediaFile>> GetSongs()
        {
            List<string> fileTypeFilter = new List<string>() { ".mp3", ".wma" };
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            StorageFolder picturesFolder = await KnownFolders.GetFolderForUserAsync(null /* current user */, KnownFolderId.MusicLibrary);
            StorageFolderQueryResult queryResult = picturesFolder.CreateFolderQueryWithOptions(queryOptions);
            IReadOnlyList<StorageFolder> folderList = await queryResult.GetFoldersAsync();
            return await GetFileListAsync(folderList, "\\DefaultAccount\\Music\\", fileTypeFilter);
        }

        private async Task<List<MediaFile>> GetImages()
        {
            List<string> fileTypeFilter = new List<string>() { ".jpeg", ".jpg", ".png", ".gif" };
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            StorageFolder picturesFolder = await KnownFolders.GetFolderForUserAsync(null /* current user */, KnownFolderId.PicturesLibrary);
            StorageFolderQueryResult queryResult = picturesFolder.CreateFolderQueryWithOptions(queryOptions);
            IReadOnlyList<StorageFolder> folderList = await queryResult.GetFoldersAsync();
            return await GetFileListAsync(folderList, "\\DefaultAccount\\Pictures\\", fileTypeFilter);
        }

        private async Task<List<MediaFile>> GetVideos()
        {
            List<string> fileTypeFilter = new List<string>() { ".mp4", ".mov", ".flv", ".avi" };
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);
            StorageFolder picturesFolder = await KnownFolders.GetFolderForUserAsync(null /* current user */, KnownFolderId.VideosLibrary);
            StorageFolderQueryResult queryResult = picturesFolder.CreateFolderQueryWithOptions(queryOptions);
            IReadOnlyList<StorageFolder> folderList = await queryResult.GetFoldersAsync();
            return await GetFileListAsync(folderList, "\\DefaultAccount\\Videos\\", fileTypeFilter);
        }

        async Task<List<MediaFile>> GetFileListAsync(IReadOnlyList<StorageFolder> folderList, string folderFilter, List<string> fileTypeFilter)
        {
            List<MediaFile> list = new List<MediaFile>();
            foreach (StorageFolder folder in folderList)
            {
                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
                foreach (StorageFile file in fileList)
                {
                    if (fileTypeFilter.Contains(file.FileType))
                    {
                        list.Add(GetMediaFile(file, folderFilter));
                    }
                }
            }
            return list;
        }

        private MediaFile GetMediaFile(StorageFile file, string mediaFilter)
        {
            return new MediaFile()
            {
                //Name = file.Name,
                Path = Uri.EscapeUriString(file.Path.Split(new string[] { mediaFilter }, StringSplitOptions.None)[1].Replace("/", "").Replace("\\", "/")),
                //ContentType = file.ContentType,
                // IsAvailable = file.IsAvailable,
                DisplayName = file.DisplayName,
                //FileType = file.FileType,
            };
        }
    }

    public sealed class MediaFile
    {
        public string DisplayName { get; set; }
        //public string ContentType { get; set; }
        //public string Name { get; set; }
        public string Path { get; set; }
        //public bool IsAvailable { get; set; }
        //public string FileType { get; set; }
    }
}
