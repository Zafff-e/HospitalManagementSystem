using Hms.Application.Emr.Dtos;

namespace Hms.Infrastructure.Clinical;

public static class Icd10Dataset
{
    public static readonly IReadOnlyList<Icd10CatalogueItemDto> Items = new List<Icd10CatalogueItemDto>
    {
        new("A09", "Infectious gastroenteritis and colitis, unspecified", "সংক্রামক গ্যাস্ট্রোএন্টেরাইটিস", "Infectious Diseases"),
        new("A90", "Dengue fever (classical dengue)", "ডেঙ্গু জ্বর", "Infectious Diseases"),
        new("B50", "Plasmodium falciparum malaria", "ম্যালেরিয়া (ফ্যালসিপেরাম)", "Infectious Diseases"),
        new("B01.9", "Varicella without complication (Chickenpox)", "জলবসন্ত", "Infectious Diseases"),
        new("B02.9", "Zoster without complication (Shingles)", "হারপিস জোস্টার", "Infectious Diseases"),
        new("J06.9", "Acute upper respiratory infection, unspecified", "তীব্র ঊর্ধ্ব শ্বাসতন্ত্রের সংক্রমণ (সর্দি-কাশি)", "Respiratory"),
        new("J18.9", "Pneumonia, unspecified organism", "নিউমোনিয়া", "Respiratory"),
        new("J45.9", "Asthma, unspecified", "হাঁপানি / ব্রঙ্কিয়াল অ্যাজমা", "Respiratory"),
        new("J20.9", "Acute bronchitis, unspecified", "তীব্র ব্রঙ্কাইটিস", "Respiratory"),
        new("I10", "Essential (primary) hypertension", "উচ্চ রক্তচাপ (হাইপারটেনশন)", "Cardiovascular"),
        new("I20.9", "Angina pectoris, unspecified", "হৃদশূল / এনজাইনা পেক্টোরিস", "Cardiovascular"),
        new("I21.9", "Acute myocardial infarction, unspecified", "তীব্র হার্ট অ্যাটাক (মায়োকার্ডিয়াল ইনফার্কশন)", "Cardiovascular"),
        new("I50.9", "Heart failure, unspecified", "হার্ট ফেইলিওর", "Cardiovascular"),
        new("E11.9", "Type 2 diabetes mellitus without complications", "টাইপ ২ ডায়াবেটিস মেলিটাস", "Endocrine"),
        new("E03.9", "Hypothyroidism, unspecified", "হাইপোথাইরয়েডিজম", "Endocrine"),
        new("E66.9", "Obesity, unspecified", "স্থূলতা / ওবেসিটি", "Endocrine"),
        new("K29.7", "Gastritis, unspecified", "গ্যাস্ট্রাইটিস / গ্যাস্ট্রিক সমস্যা", "Gastrointestinal"),
        new("K21.9", "Gastro-oesophageal reflux disease (GERD)", "এসিডিটি / অ্যাসিড রিফ্লাক্স", "Gastrointestinal"),
        new("K25.9", "Gastric ulcer, unspecified", "পাকস্থলীর আলসার", "Gastrointestinal"),
        new("K35.8", "Acute appendicitis, other and unspecified", "তীব্র অ্যাপেন্ডিসাইটিস", "Gastrointestinal"),
        new("K80.2", "Calculus of gallbladder (Gallstones)", "পিত্তথলির পাথর", "Gastrointestinal"),
        new("N39.0", "Urinary tract infection, site not specified", "মূত্রনালীর সংক্রমণ (ইউটিআই)", "Nephrology/Urology"),
        new("N20.1", "Calculus of ureter / Kidney stone", "কিডনির পাথর", "Nephrology/Urology"),
        new("M54.5", "Low back pain", "কোমরের তীব্র ও দীর্ঘমেয়াদী ব্যথা", "Musculoskeletal"),
        new("M79.1", "Myalgia", "মাংসপেশির ব্যথা", "Musculoskeletal"),
        new("M19.9", "Osteoarthritis, unspecified", "অস্টিওআর্থারাইটিস / অস্থিসন্ধির বাত", "Musculoskeletal"),
        new("R50.9", "Fever, unspecified", "অজানা উৎসের সাধারণ জ্বর", "General/Symptom"),
        new("R51", "Headache", "মাথাব্যথা / মাইগ্রেন", "General/Symptom"),
        new("R10.4", "Other and unspecified abdominal pain", "পেটে ব্যথা", "General/Symptom"),
        new("L30.9", "Dermatitis, unspecified / Eczema", "একজিমা / ত্বকের প্রদাহ", "Dermatology"),
        new("L70.0", "Acne vulgaris", "ব্রণ", "Dermatology"),
        new("H10.9", "Conjunctivitis, unspecified", "চোখ ওঠা (কনজাংটিভাইটিস)", "Ophthalmology"),
        new("D50.9", "Iron deficiency anaemia, unspecified", "রক্তস্বল্পতা (আয়রনের ঘাটতিজনিত)", "Hematology"),
        new("F41.9", "Anxiety disorder, unspecified", "উদ্বেগ / অ্যানজাইটি ডিজঅর্ডার", "Psychiatry"),
        new("F32.9", "Depressive episode, unspecified", "বিষণ্ণতা / ডিপ্রেশন", "Psychiatry")
    };
}
