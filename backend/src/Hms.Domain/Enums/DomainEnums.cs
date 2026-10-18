namespace Hms.Domain.Enums;

public enum Gender { Male, Female, Other }

public enum BloodGroup { APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative, OPositive, ONegative, Unknown }

public enum MaritalStatus { Single, Married, Divorced, Widowed, Separated, Other }

public enum BedStatus { Free, Occupied, Cleaning, Maintenance, Reserved }

public enum WardType { General, Cabin, ICU, CCU, NICU, Emergency }

public enum AppointmentStatus { Booked, CheckedIn, InConsult, Completed, Cancelled, NoShow }

public enum EncounterType { OPD, IPD, ER }

public enum EncounterStatus { Active, Completed, Cancelled }

public enum AdmissionStatus { Active, Discharged, Transferred, LAMA }

public enum TriageLevel
{
    Resuscitation = 1,  // Red
    Emergency = 2,       // Orange
    Urgent = 3,          // Yellow
    Standard = 4,        // Green
    NonUrgent = 5        // Blue
}

public enum LabOrderStatus { Ordered, SampleCollected, InProgress, ResultEntered, Verified, Cancelled }

public enum LabResultFlag { Normal, High, Low, Critical }

public enum InvoiceStatus { Draft, Issued, PartPaid, Paid, Void }

public enum PaymentMethod { Cash, Card, bKash, Nagad, Rocket, Deposit }

public enum PaymentStatus { Pending, Completed, Failed, Refunded }

public enum SurgeryStatus { Scheduled, InProgress, Completed, Cancelled, Postponed }

public enum BloodUnitStatus { Available, Reserved, Issued, Discarded, Expired }

public enum AmbulanceTripStatus { Requested, Dispatched, OnTrip, Completed, Cancelled }

public enum ErCaseStatus { Registered, InTriage, UnderTreatment, Admitted, Discharged, LAMA }

public enum DischargeSummaryStatus { Draft, Completed, Signed }

public enum StockBatchStatus { Active, Expired, Depleted }

public enum DispensationStatus { Pending, Dispensed, PartiallyDispensed, Cancelled }

public enum MedicineForm { Tablet, Capsule, Syrup, Injection, Cream, Ointment, Drops, Inhaler, Suppository, Other }

public enum DayOfWeekBd { Saturday, Sunday, Monday, Tuesday, Wednesday, Thursday, Friday }
