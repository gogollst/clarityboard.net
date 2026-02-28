export type DepreciationMethod = 'straight_line' | 'declining_balance';
export type AssetStatus = 'active' | 'disposed' | 'fully_depreciated';

export interface FixedAsset {
  id: string;
  entityId: string;
  name: string;
  assetNumber: string;
  category: string;
  acquisitionDate: string;
  acquisitionCost: number;
  usefulLifeMonths: number;
  depreciationMethod: DepreciationMethod;
  currentBookValue: number;
  status: AssetStatus;
  schedule: DepreciationEntry[];
}

export interface DepreciationEntry {
  date: string;
  amount: number;
  accumulatedDepreciation: number;
  bookValue: number;
}

export interface Anlagenspiegel {
  year: number;
  categories: AnlagenspiegelCategory[];
  total: AnlagenspiegelRow;
}

export interface AnlagenspiegelCategory {
  name: string;
  row: AnlagenspiegelRow;
}

export interface AnlagenspiegelRow {
  openingCost: number;
  additions: number;
  disposals: number;
  closingCost: number;
  openingDepreciation: number;
  depreciationCharge: number;
  disposalDepreciation: number;
  closingDepreciation: number;
  netBookValue: number;
}

export interface RegisterAssetRequest {
  entityId: string;
  name: string;
  category: string;
  acquisitionDate: string;
  acquisitionCost: number;
  usefulLifeMonths: number;
  depreciationMethod: DepreciationMethod;
}

export interface DisposeAssetRequest {
  id: string;
  disposalDate: string;
  disposalAmount: number;
}
