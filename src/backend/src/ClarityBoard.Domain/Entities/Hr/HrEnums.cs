namespace ClarityBoard.Domain.Entities.Hr;

public enum EmployeeType { Employee, Contractor }
public enum EmployeeStatus { Active, OnLeave, Terminated }
public enum SalaryType { Monthly, Hourly, DailyRate }
public enum ContractType { Permanent, FixedTerm, Freelance, WorkingStudent }
public enum EntryType { Work, Overtime, OnCall }
public enum WorkTimeStatus { Open, Locked }
public enum LeaveRequestStatus { Pending, Approved, Rejected, Cancelled }
public enum ReviewType { Annual, Probation, Quarterly, ThreeSixty }
public enum ReviewStatus { Draft, InProgress, Completed }
public enum TravelExpenseStatus { Draft, Submitted, Approved, Reimbursed, Rejected }
public enum DocumentType { Contract, Certificate, IdCopy, Payslip, Other }
public enum DeletionRequestStatus { Pending, Completed, Blocked }
public enum RespondentType { Self, Peer, Manager, DirectReport }
public enum ExpenseType { Accommodation, Transport, Meal, Other }
public enum Gender { Male, Female, Diverse, NotSpecified }
public enum EmploymentType { FullTime, PartTime, WorkingStudent, MiniJob, Internship }
