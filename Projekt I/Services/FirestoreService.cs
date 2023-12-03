using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Routing.Template;
using Projekt_I.Data;
using Projekt_I.Data.Models;
using System.Linq;
using System.Security.Claims;

namespace Projekt_I.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly UserContextService _userContextService;

        public FirestoreService(UserContextService userContextService, FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
            _userContextService = userContextService;
        }

        public async Task StoreUserData(User userData)
        {
            var usersCollection = _firestoreDb.Collection("users");

            var userDocument = usersCollection.Document(userData.Id);

            var userDocumentSnapshot = await userDocument.GetSnapshotAsync();

            if (!userDocumentSnapshot.Exists)
            {
                await userDocument.SetAsync(userData);
            }
        }

        public async Task<User> GetUserData(string userId)
        {
            var usersCollection = _firestoreDb.Collection("users");
            var userDocument = usersCollection.Document(userId);

            var userDocumentSnapshot = await userDocument.GetSnapshotAsync();

            var user = userDocumentSnapshot.ConvertTo<User>();

            return user;

        }

        public async Task<List<Novel>> GetAllUserWrittenNovels(string userId)
        {
            var novelsCollectionRef = _firestoreDb.Collection("novels");
            var query = novelsCollectionRef.WhereEqualTo("AuthorId", userId);

            var novelsSnapshot = await query.GetSnapshotAsync();

            List<Novel> novels = new List<Novel>();

            if (novelsSnapshot.Any())
            {

                foreach (var document in novelsSnapshot)
                {
                    var serializedNovel = document.ConvertTo<Novel>();
                    novels.Add(serializedNovel);
                }

                return novels;
            }

            return novels;
        }

        public async Task<Novel> GetNovel(string novelId)
        {
            var novelDocument = _firestoreDb.Collection("novels").Document(novelId);
            var novelSnapshot = await novelDocument.GetSnapshotAsync();

            return novelSnapshot.ConvertTo<Novel>();
        }

        public async Task<List<Novel>> GetAllNovels()
        {

            var novelsCollectionRef = _firestoreDb.Collection("novels");

            var novelsSnapshot = await novelsCollectionRef.GetSnapshotAsync();

            var novels = new List<Novel>();

            if (novelsSnapshot.Any())
            {
                foreach (var document in novelsSnapshot)
                {
                    var serializedVolume = document.ConvertTo<Novel>();
                    novels.Add(serializedVolume);
                }

                return novels;
            }

            return novels;

        }

        public async Task<List<Novel>> GetNovelsByTags(string[] tags, bool includeAll)
        {
            var novelsCollectionRef = _firestoreDb.Collection("novels");
            var novels = new List<Novel>();

            if (tags == null || tags.Length == 0)
            {
                return novels;
            }


            var query = novelsCollectionRef.WhereArrayContainsAny("Tags", tags);

            if (includeAll)
            {
                var filteredOutList = await ExecuteQueryAsync(query);
                return filteredOutList.Where(novel => tags.All(tag => novel.Tags.Contains(tag))).ToList();
            }


            return await ExecuteQueryAsync(query);
        }

        public async Task<List<Novel>> GetNovelsByName(string searchString)
        {
            var novelsCollectionRef = _firestoreDb.Collection("novels");

            var query = novelsCollectionRef.OrderBy("Title").StartAt(searchString).EndAt(searchString + '\uf8ff');
            return await ExecuteQueryAsync(query);
        }

        private async Task<List<Novel>> ExecuteQueryAsync(Query query)
        {
            var novels = new List<Novel>();
            var querySnapshot = await query.GetSnapshotAsync();

            if (querySnapshot.Any())
            {
                foreach (var document in querySnapshot)
                {
                    var serializedVolume = document.ConvertTo<Novel>();
                    novels.Add(serializedVolume);
                }
            }

            return novels;
        }

        public async Task<List<Volume>> GetVolumesFromNovel(string novelId)
        {
            var volumesCollectionRef = _firestoreDb.Collection("novels").Document(novelId).Collection("volumes");

            var volumesSnapshot = await volumesCollectionRef.GetSnapshotAsync();

            var volumes = new List<Volume>();

            if (volumesSnapshot.Any())
            {
                foreach (var document in volumesSnapshot)
                {
                    var serializedVolume = document.ConvertTo<Volume>();
                    volumes.Add(serializedVolume);
                }

                return volumes;
            }

            return volumes;
        }

        public async Task<List<Chapter>> GetChaptersFromVolume(string novelId, string volumeId)
        {
            var chaptersCollectionRef = _firestoreDb.Collection("novels")
                .Document(novelId)
                .Collection("volumes")
                .Document(volumeId)
                .Collection("chapters");

            var chaptersSnapshot = await chaptersCollectionRef.GetSnapshotAsync();

            var chapters = new List<Chapter>();

            if (chaptersSnapshot.Any())
            {
                foreach (var document in chaptersSnapshot)
                {
                    var serializedChapter = document.ConvertTo<Chapter>();
                    chapters.Add(serializedChapter);
                }
                return chapters;
            }
            return chapters;
        }

        public async Task<Content> GetContentFromChapter(string novelId, string volumeId, string chapterId)
        {
            var contentCollectionRef = _firestoreDb.Collection("novels")
                .Document(novelId)
                .Collection("volumes")
                .Document(volumeId)
                .Collection("chapters")
                .Document(chapterId)
                .Collection("content");

            var contentSnapshot = await contentCollectionRef.GetSnapshotAsync();

            var document = contentSnapshot.FirstOrDefault().ConvertTo<Content>();
            return document ?? null;
        }

        public async Task<bool> CreateNovel(string userId, string title, string description, string[] tags)
        {
            Novel novel = new Novel()
            {
                AuthorId = userId,
                AuthorUsername = _userContextService.GetUser().FindFirstValue(ClaimTypes.GivenName),
                Title = title,
                Description = description,
                Tags = tags
            };

            var result = await _firestoreDb.Collection("novels").AddAsync(novel);

            if (result != null)
            {
                novel.Id = result.Id;

                await result.SetAsync(novel);

                return true;
            }

            return false;
        }

        public async Task<Volume> AddVolumeToNovel(string userId, string title, string novelId)
        {
            Volume volume;
            var novelDocument = _firestoreDb.Collection("novels").Document(novelId);
            var novelSnapshot = await novelDocument.GetSnapshotAsync();

            var novel = novelSnapshot.ConvertTo<Novel>();

            if (novel.AuthorId == userId)
            {
                var volumesRef = novelDocument.Collection("volumes");

                var volumesSnapshot = await volumesRef.GetSnapshotAsync();

                List<Volume> volumes = new List<Volume>();

                if (volumesSnapshot != null)
                {
                    foreach (var document in volumesSnapshot)
                    {
                        volumes.Add(document.ConvertTo<Volume>());
                    }

                    int newestNumber = volumes.Any() ? volumes.Max(x => x.Number) : 0;

                    volume = new Volume()
                    {
                        Number = newestNumber + 1,
                        Title = title,
                    };
                    var result = await volumesRef.AddAsync(volume);
                    if (result != null)
                    {
                        volume.Id = result.Id;

                        await result.SetAsync(volume);

                        return volume;
                    }
                }
            }

            return null;
        }

        public async Task<bool> RemoveVolumeFromNovel(string userId, string novelId, string volumeId)
        {

            var novelDocument = _firestoreDb.Collection("novels").Document(novelId);


            if (await IsUserAuthorOfNovel(userId, novelId))
            {
                var volumesRef = novelDocument.Collection("volumes");

                var volumeToDeleteQuery = volumesRef.WhereEqualTo("Id", volumeId);
                var volumeToDeleteSnapshot = await volumeToDeleteQuery.GetSnapshotAsync();

                if (volumeToDeleteSnapshot != null && volumeToDeleteSnapshot.Count > 0)
                {
                    var volumeDocument = volumeToDeleteSnapshot.Documents.First().Reference;

                    await volumeDocument.DeleteAsync();


                    var remainingVolumesQuery = await volumesRef.OrderBy("Number").GetSnapshotAsync();

                    int newNumber = 1;
                    foreach (var remainingVolumeDocument in remainingVolumesQuery.Documents)
                    {
                        var remainingVolume = remainingVolumeDocument.ConvertTo<Volume>();
                        var remainingVolumeReference = remainingVolumeDocument.Reference;

                        remainingVolume.Number = newNumber++;

                        await remainingVolumeReference.SetAsync(remainingVolume);
                    }
                    return true;
                }
            }

            return false;

        }

        public async Task<bool> RemoveChapterFromVolume(string userId, string novelId, string volumeId, string chapterId)
        {
            if (await IsUserAuthorOfNovel(userId, novelId))
            {
                var chaptersCollectionRef = _firestoreDb.Collection("novels")
                    .Document(novelId)
                    .Collection("volumes")
                    .Document(volumeId)
                    .Collection("chapters");


                var chapterDocument = await chaptersCollectionRef.Document(chapterId).GetSnapshotAsync();
                if (chapterDocument.Exists)
                {

                    var contentSubcollectionRef = chapterDocument.Reference.Collection("content");
                    var contentDocs = await contentSubcollectionRef.ListDocumentsAsync().ToListAsync();

                    foreach (var contentDoc in contentDocs)
                    {
                        await contentDoc.DeleteAsync();
                    }

                    await chapterDocument.Reference.DeleteAsync();


                    var remainingChaptersQuery = await chaptersCollectionRef.OrderBy("Number").GetSnapshotAsync();

                    int newNumber = 1;


                    foreach (var remainingChapterDocument in remainingChaptersQuery.Documents)
                    {
                        var remainingChapter = remainingChapterDocument.ConvertTo<Chapter>();
                        var remainingChapterReference = remainingChapterDocument.Reference;

                        remainingChapter.Number = newNumber++;


                        await remainingChapterReference.SetAsync(remainingChapter);
                    }

                    return true;
                }
            }

            return false;
        }

        public async Task<Chapter> AddChapterToVolume(string userId, string novelId, string volumeId, string chapterTitle)
        {
            if (await IsUserAuthorOfNovel(userId, novelId))
            {
                var chaptersCollectionRef = _firestoreDb.Collection("novels")
                    .Document(novelId)
                    .Collection("volumes")
                    .Document(volumeId)
                    .Collection("chapters");

                var currentChapters = await GetChaptersFromVolume(novelId, volumeId);

                int newChapterNumber = currentChapters.Any() ? currentChapters.Max(x => x.Number) + 1 : 1;

                var newChapter = new Chapter
                {
                    Title = chapterTitle,
                    Number = newChapterNumber,
                };

                var chapterResult = await chaptersCollectionRef.AddAsync(newChapter);

                if (chapterResult != null)
                {

                    newChapter.Id = chapterResult.Id;
                    await chapterResult.SetAsync(newChapter);

                    var contentRef = _firestoreDb.Collection("novels")
                        .Document(novelId)
                        .Collection("volumes")
                        .Document(volumeId)
                        .Collection("chapters")
                        .Document(newChapter.Id)
                        .Collection("content")
                        .Document();

                    var content = new Content
                    {
                        Id = contentRef.Id,
                        ChapterContent = " ",
                    };

                    var contentResult = await contentRef.SetAsync(content);

                    if (contentResult != null)
                    {
                        return newChapter;
                    }
                }
            }

            return null;
        }

        public async Task<bool> UpdateContentInChapter(string userId, string novelId, string volumeId, string chapterId, string contentId, string newContent)
        {

            if (await IsUserAuthorOfNovel(userId, novelId))
            {
                var contentDocumentRef = _firestoreDb.Collection("novels")
                    .Document(novelId)
                    .Collection("volumes")
                    .Document(volumeId)
                    .Collection("chapters")
                    .Document(chapterId)
                    .Collection("content")
                    .Document(contentId);

                var contentSnapshot = await contentDocumentRef.GetSnapshotAsync();

                if (contentSnapshot.Exists)
                {
                    var updatedContent = new Content() { ChapterContent = newContent, Id = contentId };


                    await contentDocumentRef.SetAsync(updatedContent);

                    return true;
                }
            }

            return false;
        }

        private async Task<bool> IsUserAuthorOfNovel(string userId, string novelId)
        {
            var novelDocument = _firestoreDb.Collection("novels").Document(novelId);
            var novelSnapshot = await novelDocument.GetSnapshotAsync();

            if (novelSnapshot.Exists)
            {
                var novel = novelSnapshot.ConvertTo<Novel>();
                return novel.AuthorId == userId;
            }

            return false;
        }

        private CollectionReference GetLibraryCollection()
        {
            return _firestoreDb.Collection("libraries");
        }

        public async Task<bool> SaveUserLibrary(string userId, string novelId, string volumeId, string chapterId, int bookmark)
        {
            try
            {

                var existingDocument = await GetLibraryCollection()
                    .WhereEqualTo("ReaderId", userId)
                    .GetSnapshotAsync();


                if (existingDocument.Count > 0)
                {
                    var documentReference = existingDocument.Documents[0].Reference;


                    await documentReference.SetAsync(new Library
                    {
                        ReaderId = userId,
                        NovelId = novelId,
                        VolumeId = volumeId,
                        ChapterId = chapterId,
                        Bookmark = bookmark
                    });
                }
                else
                {
                    await GetLibraryCollection().AddAsync(new Library
                    {
                        ReaderId = userId,
                        NovelId = novelId,
                        VolumeId = volumeId,
                        ChapterId = chapterId,
                        Bookmark = bookmark
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user library: {ex.Message}");
                return false;
            }
        }

        public async Task<Library> GetUserLibrary(string userId)
        {
            try
            {
                var query = await GetLibraryCollection()
                    .WhereEqualTo("ReaderId", userId)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (query.Count > 0)
                {
                    var library = query.Documents[0].ConvertTo<Library>();
                    return library;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user library: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RemoveNovel(string userId, string novelId)
        {
            var novelDocument = _firestoreDb.Collection("novels").Document(novelId);

            if (await IsUserAuthorOfNovel(userId, novelId))
            {
                var volumesRef = novelDocument.Collection("volumes");
                var volumesQuery = await volumesRef.GetSnapshotAsync();

                foreach (var volume in volumesQuery.Documents)
                {
                    var volumeDocument = volumesRef.Document(volume.Id);

                    var chaptersRef = volumeDocument.Collection("chapters");
                    var chaptersQuery = await chaptersRef.GetSnapshotAsync();

                    foreach (var chapter in chaptersQuery.Documents)
                    {
                        var chapterDocument = chaptersRef.Document(chapter.Id);

                        var contentRef = chapterDocument.Collection("content");
                        var contentQuery = await contentRef.GetSnapshotAsync();

                        foreach (var contentDocument in contentQuery.Documents)
                        {
                            await contentDocument.Reference.DeleteAsync();
                        }

                        await chapterDocument.DeleteAsync();
                    }

                    await volumeDocument.DeleteAsync();
                }

                await novelDocument.DeleteAsync();

                return true;
            }

            return false;
        }
        public async Task<List<Novel>> SearchNovelsByTags(string[] tags)
        {
            var novelsCollectionRef = _firestoreDb.Collection("novels");

            var query = novelsCollectionRef.WhereArrayContainsAny("Tags", tags);

            var novelsSnapshot = await query.GetSnapshotAsync();

            var novels = new List<Novel>();

            foreach (var document in novelsSnapshot)
            {
                var serializedNovel = document.ConvertTo<Novel>();
                novels.Add(serializedNovel);
            }

            return novels;
        }

        public async Task<bool> MarkNovelAsFavorite(string userId, string novelId)
        {
            try
            {
                var favoritesCollectionRef = _firestoreDb.Collection("favorites");

                var userFavoriteDocument = await favoritesCollectionRef.Document(userId).GetSnapshotAsync();

                if (userFavoriteDocument != null && userFavoriteDocument.Exists)
                {
                    var novelIdsArray = userFavoriteDocument.GetValue<List<string>>("novelIds") ?? new List<string>();
                    novelIdsArray.Add(novelId);

                    await userFavoriteDocument.Reference.SetAsync(new Dictionary<string, object>
                    {
                        { "readerId", userId },
                        { "novelIds", novelIdsArray }
                    }, SetOptions.MergeAll);
                }
                else
                {
                    await favoritesCollectionRef.Document(userId).SetAsync(new Dictionary<string, object>
                    {
                        { "readerId", userId },
                        { "novelIds", new List<string> { novelId } }
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking novel as favorite: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Novel>> GetFavoriteNovels(string userId)
        {
            try
            {
                var favoritesCollectionRef = _firestoreDb.Collection("favorites");

                var userFavoriteDocument = await favoritesCollectionRef.Document(userId).GetSnapshotAsync();

                if (userFavoriteDocument != null && userFavoriteDocument.Exists)
                {
                    var novelIdsArray = userFavoriteDocument.GetValue<List<string>>("novelIds");

                    if (novelIdsArray != null && novelIdsArray.Any())
                    {
                        var novels = new List<Novel>();

                        foreach (var novelId in novelIdsArray)
                        {
                            var novel = await GetNovel(novelId);

                            if (novel != null)
                            {
                                novels.Add(novel);
                            }
                        }

                        return novels;
                    }
                }

                return new List<Novel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving favorite novels: {ex.Message}");
                return new List<Novel>();
            }
        }

        public async Task<bool> MarkNovelAsPlanToRead(string userId, string novelId)
        {
            try
            {
                var planToReadCollectionRef = _firestoreDb.Collection("planToRead");

                var userPlanToReadDocument = await planToReadCollectionRef.Document(userId).GetSnapshotAsync();

                if (userPlanToReadDocument != null && userPlanToReadDocument.Exists)
                {
                    var novelIdsArray = userPlanToReadDocument.GetValue<List<string>>("novelIds") ?? new List<string>();
                    novelIdsArray.Add(novelId);

                    await userPlanToReadDocument.Reference.SetAsync(new Dictionary<string, object>
                    {
                        { "readerId", userId },
                        { "novelIds", novelIdsArray },
                        { "timestamp", FieldValue.ServerTimestamp }
                    }, SetOptions.MergeAll);
                }
                else
                {
                    await planToReadCollectionRef.Document(userId).SetAsync(new Dictionary<string, object>
                    {
                        { "readerId", userId },
                        { "novelIds", new List<string> { novelId } },
                        { "timestamp", FieldValue.ServerTimestamp }
                    });
                }

                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error marking novel as plan to read: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Novel>> GetPlanToReadNovels(string userId)
        {
            try
            {
                var planToReadCollectionRef = _firestoreDb.Collection("planToRead");

                var userPlanToReadDocument = await planToReadCollectionRef.Document(userId).GetSnapshotAsync();

                if (userPlanToReadDocument != null && userPlanToReadDocument.Exists)
                {
                    var novelIdsArray = userPlanToReadDocument.GetValue<List<string>>("novelIds");

                    if (novelIdsArray != null && novelIdsArray.Any())
                    {
                        var novels = new List<Novel>();

                        foreach (var novelId in novelIdsArray)
                        {
                            var novel = await GetNovel(novelId);

                            if (novel != null)
                            {
                                novels.Add(novel);
                            }
                        }

                        return novels;
                    }
                }

                return new List<Novel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving plan to read novels: {ex.Message}");
                return new List<Novel>();
            }
        }

        public async Task<bool> CheckIfNovelInFavorites(string userId, string novelId)
        {
            try
            {
                var favoritesCollectionRef = _firestoreDb.Collection("favorites");

                var userFavoriteDocument = await favoritesCollectionRef.Document(userId).GetSnapshotAsync();

                if (userFavoriteDocument != null && userFavoriteDocument.Exists)
                {
                    var novelIdsArray = userFavoriteDocument.GetValue<List<string>>("novelIds");

                    return novelIdsArray != null && novelIdsArray.Contains(novelId);
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if novel is in favorites: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckIfNovelInPlanToRead(string userId, string novelId)
        {
            try
            {
                var planToReadCollectionRef = _firestoreDb.Collection("planToRead");

                var userPlanToReadDocument = await planToReadCollectionRef.Document(userId).GetSnapshotAsync();

                if (userPlanToReadDocument != null && userPlanToReadDocument.Exists)
                {
                    var novelIdsArray = userPlanToReadDocument.GetValue<List<string>>("novelIds");

                    return novelIdsArray != null && novelIdsArray.Contains(novelId);
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if novel is in plan to read: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveNovelFromFavorites(string userId, string novelId)
        {
            try
            {
                var favoritesCollectionRef = _firestoreDb.Collection("favorites");

                var userFavoriteDocument = await favoritesCollectionRef.Document(userId).GetSnapshotAsync();

                if (userFavoriteDocument != null && userFavoriteDocument.Exists)
                {
                    var novelIdsArray = userFavoriteDocument.GetValue<List<string>>("novelIds");

                    if (novelIdsArray != null)
                    {
                        novelIdsArray.Remove(novelId);

                        await userFavoriteDocument.Reference.SetAsync(new Dictionary<string, object>
                        {
                            { "readerId", userId },
                            { "novelIds", novelIdsArray }
                        }, SetOptions.MergeAll);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing novel from favorites: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveNovelFromPlanToRead(string userId, string novelId)
        {
            try
            {
                var planToReadCollectionRef = _firestoreDb.Collection("planToRead");

                var userPlanToReadDocument = await planToReadCollectionRef.Document(userId).GetSnapshotAsync();

                if (userPlanToReadDocument != null && userPlanToReadDocument.Exists)
                {
                    var novelIdsArray = userPlanToReadDocument.GetValue<List<string>>("novelIds");

                    if (novelIdsArray != null)
                    {
                        novelIdsArray.Remove(novelId);

                        await userPlanToReadDocument.Reference.SetAsync(new Dictionary<string, object>
                        {
                            { "readerId", userId },
                            { "novelIds", novelIdsArray },
                            { "timestamp", FieldValue.ServerTimestamp } 
                        }, SetOptions.MergeAll);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing novel from plan to read: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateNovel(string userId, string novelId, string title, string description, string[] tags)
        {
            try
            {
                var novelDocument = _firestoreDb.Collection("novels").Document(novelId);

                if (await IsUserAuthorOfNovel(userId, novelId))
                {
                    var novelSnapshot = await novelDocument.GetSnapshotAsync();

                    if (novelSnapshot.Exists)
                    {
                        var existingNovel = novelSnapshot.ConvertTo<Novel>();

                        existingNovel.Title = title;
                        existingNovel.Description = description;
                        existingNovel.Tags = tags;

                        await novelDocument.SetAsync(existingNovel);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating novel: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateVolume(string userId, string novelId, string volumeId, string title)
        {
            try
            {
                var volumeDocument = _firestoreDb.Collection("novels").Document(novelId).Collection("volumes").Document(volumeId);

                if (await IsUserAuthorOfNovel(userId, novelId))
                {
                    var volumeSnapshot = await volumeDocument.GetSnapshotAsync();

                    if (volumeSnapshot.Exists)
                    {
                        var existingVolume = volumeSnapshot.ConvertTo<Volume>();

                        existingVolume.Title = title;

                        await volumeDocument.SetAsync(existingVolume);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating volume: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateChapter(string userId, string novelId, string volumeId, string chapterId, string title)
        {
            try
            {
                var chapterDocument = _firestoreDb.Collection("novels").Document(novelId).Collection("volumes").Document(volumeId).Collection("chapters").Document(chapterId);

                if (await IsUserAuthorOfNovel(userId, novelId))
                {
                    var chapterSnapshot = await chapterDocument.GetSnapshotAsync();

                    if (chapterSnapshot.Exists)
                    {
                        var existingChapter = chapterSnapshot.ConvertTo<Chapter>();

                        existingChapter.Title = title;

                        await chapterDocument.SetAsync(existingChapter);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating chapter: {ex.Message}");
                return false;
            }
        }

    }

}
