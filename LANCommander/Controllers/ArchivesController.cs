﻿using LANCommander.Data;
using LANCommander.Data.Models;
using LANCommander.Extensions;
using LANCommander.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LANCommander.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ArchivesController : Controller
    {
        private readonly DatabaseContext Context;

        public ArchivesController(DatabaseContext context)
        {
            Context = context;
        }

        public async Task<IActionResult> Add(Guid? id)
        {
            if (id == null || Context.Games == null)
                return NotFound();

            using (Repository<Game> repo = new Repository<Game>(Context, HttpContext))
            {
                var game = await repo.Find(id.GetValueOrDefault());

                Archive lastVersion = null;

                if (game.Archives != null && game.Archives.Count > 0)
                    lastVersion = game.Archives.OrderByDescending(a => a.CreatedOn).FirstOrDefault();

                return View(new Archive()
                {
                    Game = game,
                    LastVersion = lastVersion,
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(Guid? id, Archive archive)
        {
            archive.Id = Guid.Empty;

            using (Repository<Game> gameRepo = new Repository<Game>(Context, HttpContext))
            {
                var game = await gameRepo.Find(id.GetValueOrDefault());

                archive.Game = game;

                if (game.Archives != null && game.Archives.Any(a => a.Version == archive.Version))
                    ModelState.AddModelError("Version", "An archive for this game is already using that version.");

                if (ModelState.IsValid)
                {
                    using (Repository<Archive> archiveRepo = new Repository<Archive>(Context, HttpContext))
                    {
                        archive = await archiveRepo.Add(archive);
                        await archiveRepo.SaveChanges();
                    }

                    return RedirectToAction("Edit", "Games", new { id = id });
                }
            }

            return View(archive);
        }

        public async Task<IActionResult> Download(Guid id)
        {
            using (Repository<Archive> repo = new Repository<Archive>(Context, HttpContext))
            {
                var archive = await repo.Find(id);

                var content = new FileStream(Path.Combine("Upload", archive.ObjectKey), FileMode.Open, FileAccess.Read, FileShare.Read);

                return File(content, "application/octet-stream", $"{archive.Game.Title.SanitizeFilename()}.zip");
            }
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            Guid gameId;

            using (Repository<Archive> repo = new Repository<Archive>(Context, HttpContext))
            {
                var archive = await repo.Find(id.GetValueOrDefault());

                gameId = archive.Game.Id;

                System.IO.File.Delete(Path.Combine("Upload", archive.ObjectKey));

                repo.Delete(archive);

                await repo.SaveChanges();
            }

            return RedirectToAction("Edit", "Games", new { id = gameId });
        }

        public async Task<IActionResult> ValidateManifest(Guid id)
        {
            var path = Path.Combine("Upload", id.ToString());

            string manifestContents = String.Empty;

            if (!System.IO.File.Exists(path))
                return BadRequest("Specified object does not exist");

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    var manifest = archive.Entries.FirstOrDefault(e => e.FullName == "_manifest.yml");

                    if (manifest == null)
                        throw new FileNotFoundException("Manifest file not found. Add a _manifest.yml file to your archive and try again.");

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName == "_manifest.yml")
                        {
                            using (StreamReader sr = new StreamReader(entry.Open()))
                            {
                                manifestContents = await sr.ReadToEndAsync();
                            }
                        }
                    }
                }
            }
            catch (InvalidDataException ex)
            {
                System.IO.File.Delete(path);
                return BadRequest("Uploaded archive is corrupt or not a .zip file.");
            }
            catch (FileNotFoundException ex)
            {
                System.IO.File.Delete(path);
                return BadRequest(ex.Message);
            }
            catch
            {
                System.IO.File.Delete(path);
                return BadRequest("An unknown error occurred.");
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            try
            {
                var manifest = deserializer.Deserialize<GameManifest>(manifestContents);
            }
            catch
            {
                System.IO.File.Delete(path);
                return BadRequest("The manifest file is invalid or corrupt.");
            }

            return Ok();
        }
    }
}
