using OPDClinic.Models;

namespace OPDClinic.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                FullName = "System Administrator",
                Role = UserRole.Admin,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        if (!db.MedicineForms.Any())
        {
            db.MedicineForms.AddRange(
                new MedicineForm { Category = "Enteral",    FormName = "Tablet",     Abbreviation = "Tab", Note = "Solid Forms" },
                new MedicineForm { Category = "Enteral",    FormName = "Capsule",    Abbreviation = "Cap", Note = "Solid Forms" },
                new MedicineForm { Category = "Parenteral", FormName = "Injection",  Abbreviation = "Inj", Note = "Injectable Forms" },
                new MedicineForm { Category = "Parenteral", FormName = "Salin",      Abbreviation = null,  Note = "Solid Forms" },
                new MedicineForm { Category = null,         FormName = "Powder",     Abbreviation = null,  Note = "Solid Forms" },
                new MedicineForm { Category = null,         FormName = "Granule",    Abbreviation = null,  Note = "Solid Forms" },
                new MedicineForm { Category = null,         FormName = "Lozenge",    Abbreviation = null,  Note = "Solid Forms" },
                new MedicineForm { Category = "Enteral",    FormName = "Pill",       Abbreviation = null,  Note = "Solid Forms" },
                new MedicineForm { Category = "Enteral",    FormName = "Syrup",      Abbreviation = null,  Note = "Liquid Forms" },
                new MedicineForm { Category = null,         FormName = "Suspension", Abbreviation = null,  Note = "Liquid Forms" }
            );
            db.SaveChanges();
        }

        if (!db.Dosages.Any())
        {
            db.Dosages.AddRange(
                new Dosage { DosageText = "هر صبح یک عدد بعداز غذا",                    Type = "Tablet",    Category = "Enteral" },
                new Dosage { DosageText = "هر شب یک عدد بعداز غذا",                     Type = "Tablet",    Category = "Enteral" },
                new Dosage { DosageText = "صبح و شب یک یک عدد بعداز غذا",               Type = "Tablet",    Category = "Enteral" },
                new Dosage { DosageText = "صبح ، چاشت و شب یک یک عدد بعداز غذا",        Type = "Tablet",    Category = "Enteral" },
                new Dosage { DosageText = "روزانه یک امپول زرق عضلی گردد",              Type = "Injection", Category = "Parenteral" },
                new Dosage { DosageText = "روزانه یک امپول زرق وریدی گردد",             Type = "Injection", Category = "Parenteral" }
            );
            db.SaveChanges();
        }

        if (!db.MedicineNotes.Any())
        {
            db.MedicineNotes.AddRange(
                new MedicineNote { Notes = "تست حساسیت انجام شود." },
                new MedicineNote { Notes = "در مرکز صحی انجام گیرد." },
                new MedicineNote { Notes = "مطابق به هدایت داده شده مصرف گردد." },
                new MedicineNote { Notes = "در صورت ضرورت استفاده شود." }
            );
            db.SaveChanges();
        }

        if (!db.PrescriptionNotes.Any())
        {
            db.PrescriptionNotes.AddRange(
                new PrescriptionNote { Notes = "در صورت مراجعه بعدی نسخه را با خود داشته باشید." },
                new PrescriptionNote { Notes = "در مرکز صحی انجام گیرد." },
                new PrescriptionNote { Notes = "مطابق به هدایت داده شده مصرف گردد." },
                new PrescriptionNote { Notes = "بعداز ختم دوا دوباره مراجعه گردد." }
            );
            db.SaveChanges();
        }

        if (!db.Routes.Any())
        {
            db.Routes.AddRange(
                // Parenteral
                new RouteOfAdministration { RouteName = "Intravenous",       Abbreviation = "IV",  Category = "Parenteral",   Description = "Injection directly into a vein" },
                new RouteOfAdministration { RouteName = "Intramuscular",     Abbreviation = "IM",  Category = "Parenteral",   Description = "Injection into a muscle" },
                new RouteOfAdministration { RouteName = "Subcutaneous",      Abbreviation = "SC",  Category = "Parenteral",   Description = "Injection under the skin" },
                new RouteOfAdministration { RouteName = "Intradermal",       Abbreviation = "ID",  Category = "Parenteral",   Description = "Injection into the dermis layer of skin" },
                new RouteOfAdministration { RouteName = "Intra-arterial",    Abbreviation = "IA",  Category = "Parenteral",   Description = "Injection into an artery" },
                new RouteOfAdministration { RouteName = "Intrathecal",       Abbreviation = null,  Category = "Parenteral",   Description = "Injection into the spinal canal" },
                new RouteOfAdministration { RouteName = "Intraosseous",      Abbreviation = null,  Category = "Parenteral",   Description = "Injection into bone marrow" },
                new RouteOfAdministration { RouteName = "Intraperitoneal",   Abbreviation = null,  Category = "Parenteral",   Description = "Injection into the peritoneal cavity" },
                new RouteOfAdministration { RouteName = "Intra-articular",   Abbreviation = null,  Category = "Parenteral",   Description = "Injection into a joint" },
                new RouteOfAdministration { RouteName = "Intracardiac",      Abbreviation = null,  Category = "Parenteral",   Description = "Injection into the heart" },
                new RouteOfAdministration { RouteName = "Intravitreal",      Abbreviation = null,  Category = "Parenteral",   Description = "Injection into the vitreous body of the eye" },
                new RouteOfAdministration { RouteName = "Epidural",          Abbreviation = null,  Category = "Parenteral",   Description = "Injected into the epidural space of the spine" },
                // Enteral
                new RouteOfAdministration { RouteName = "Oral",              Abbreviation = "PO",  Category = "Enteral",      Description = "Taken by mouth and swallowed" },
                new RouteOfAdministration { RouteName = "Sublingual",        Abbreviation = "SL",  Category = "Enteral",      Description = "Placed under the tongue" },
                new RouteOfAdministration { RouteName = "Buccal",            Abbreviation = null,  Category = "Enteral",      Description = "Placed between the gums and cheek" },
                new RouteOfAdministration { RouteName = "Rectal",            Abbreviation = "PR",  Category = "Enteral",      Description = "Administered via the rectum" },
                // Respiratory
                new RouteOfAdministration { RouteName = "Inhalation",        Abbreviation = null,  Category = "Respiratory",  Description = "Breathed into the lungs" },
                new RouteOfAdministration { RouteName = "Intranasal",        Abbreviation = null,  Category = "Respiratory",  Description = "Administered through the nose" },
                // Topical
                new RouteOfAdministration { RouteName = "Topical",           Abbreviation = null,  Category = "Topical",      Description = "Applied directly to the skin" },
                new RouteOfAdministration { RouteName = "Transdermal",       Abbreviation = null,  Category = "Topical",      Description = "Delivered through the skin via patches" },
                new RouteOfAdministration { RouteName = "Ophthalmic",        Abbreviation = null,  Category = "Topical",      Description = "Administered into the eye" },
                new RouteOfAdministration { RouteName = "Otic",              Abbreviation = null,  Category = "Topical",      Description = "Administered into the ear" },
                new RouteOfAdministration { RouteName = "Vaginal",           Abbreviation = null,  Category = "Topical",      Description = "Administered into the vagina" },
                new RouteOfAdministration { RouteName = "Urethral",          Abbreviation = null,  Category = "Topical",      Description = "Administered into the urethra" },
                // Specialized
                new RouteOfAdministration { RouteName = "Intravesical",      Abbreviation = null,  Category = "Specialized",  Description = "Administered into the bladder" },
                new RouteOfAdministration { RouteName = "Intracavitary",     Abbreviation = null,  Category = "Specialized",  Description = "Administered into a body cavity" },
                new RouteOfAdministration { RouteName = "Intralesional",     Abbreviation = null,  Category = "Specialized",  Description = "Injected directly into a lesion" },
                new RouteOfAdministration { RouteName = "Implantation",      Abbreviation = null,  Category = "Specialized",  Description = "Drug-releasing implant under the skin" }
            );
            db.SaveChanges();
        }
    }
}
