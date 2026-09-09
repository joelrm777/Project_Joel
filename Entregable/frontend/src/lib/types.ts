export type VehicleType = 'Car' | 'Motorcycle'
export type FuelType = 'Gasoline' | 'Diesel' | 'Hybrid' | 'Electric'
export type DriveType = 'Single' | 'DoubleTraction' | 'NotApplicable'
export type MileageClaimStatus = 'Draft' | 'Pending' | 'Approved' | 'Rejected' | 'Discarded'

export interface LegDto {
  sequenceNumber: number
  originStoreId: number
  destinationStoreId: number
  appliedDistanceKm: number | null
}

export interface TripDto {
  id: string
  date: string
  appliedRatePerKm: number
  rateSummary: string
  totalDistance: number
  totalAmount: number
  isDistanceComplete: boolean
  legs: LegDto[]
}

export interface MileageClaimDto {
  id: string
  employeeNationalId: string
  employeeName: string
  employeeEmail: string
  approverNationalId: string
  approverEmail: string
  vehicleType: VehicleType
  fuelType: FuelType
  plateNumber: string
  driveType: DriveType
  modelYear: number
  engineDisplacement: number
  status: MileageClaimStatus
  createdAt: string
  submittedAt: string | null
  decidedAt: string | null
  financeReceivedAt: string | null
  rejectionReason: string | null
  totalAmount: number
  trips: TripDto[]
}

export interface Store {
  id: number
  name: string
}

export interface StoreDistance {
  id: number
  originStoreId: number
  destinationStoreId: number
  distanceKm: number
}

export interface SummaryReportRow {
  mileageClaimId: string
  employeeName: string
  totalAmount: number
  submittedAt: string
  decidedAt: string | null
  status: string
}

export interface SummaryReport {
  totalAmount: number
  claimCount: number
  averageProcessingHours: number | null
  rows: SummaryReportRow[]
}

export interface RateTableEntry {
  id: string
  vehicleType: VehicleType
  fuelType: FuelType
  engineDisplacementMin: number
  engineDisplacementMax: number
  vehicleAgeYears: number
  ratePerKm: number
}

export type UserRole = 'Employee' | 'Approver' | 'Administrator' | 'Finance'
