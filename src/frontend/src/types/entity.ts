export interface LegalEntity {
  id: string;
  name: string;
  legalForm: string;
  registrationNumber: string | null;
  taxId: string | null;
  vatId: string | null;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  currency: string;
  chartOfAccounts: 'SKR03' | 'SKR04';
  fiscalYearStartMonth: number;
  parentEntityId: string | null;
  isActive: boolean;
  datevClientNumber: string | null;
  datevConsultantNumber: string | null;
  managingDirectorId: string | null;
  managingDirectorName: string | null;
  createdAt: string;
}

export interface CreateEntityRequest {
  name: string;
  legalForm: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  currency: string;
  chartOfAccounts: string;
  fiscalYearStartMonth: number;
  parentEntityId?: string | null;
  registrationNumber?: string;
  taxId?: string;
  vatId?: string;
  datevClientNumber?: string;
  datevConsultantNumber?: string;
  managingDirectorId?: string | null;
  templateDepartmentEntityId?: string;
}

export interface UpdateEntityRequest {
  name: string;
  legalForm: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  currency: string;
  chartOfAccounts: string;
  fiscalYearStartMonth: number;
  parentEntityId?: string | null;
  registrationNumber?: string;
  taxId?: string;
  vatId?: string;
  datevClientNumber?: string;
  datevConsultantNumber?: string;
  managingDirectorId?: string | null;
}

export interface Address {
  street: string;
  city: string;
  postalCode: string;
  country: string;
}

export interface EntityRelationship {
  parentId: string;
  childId: string;
  ownershipPct: number;
  consolidationType: 'full' | 'proportional' | 'equity' | 'none';
  hasProfitTransferAgreement: boolean;
}

export interface DepartmentNode {
  id: string;
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  managerId?: string;
  managerName?: string;
  isActive: boolean;
  children: DepartmentNode[];
}
