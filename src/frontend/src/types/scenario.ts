export type ScenarioType = 'best_case' | 'worst_case' | 'custom' | 'stress_test';
export type ScenarioStatus = 'draft' | 'running' | 'completed';

export interface Scenario {
  id: string;
  entityId: string;
  name: string;
  type: ScenarioType;
  status: ScenarioStatus;
  parameters: ScenarioParameter[];
  results?: ScenarioResult[];
  createdAt: string;
}

export interface ScenarioParameter {
  kpiId: string;
  adjustmentType: 'absolute' | 'percentage';
  adjustmentValue: number;
}

export interface ScenarioResult {
  kpiId: string;
  baselineValue: number;
  projectedValue: number;
  delta: number;
}

export interface CreateScenarioRequest {
  entityId: string;
  name: string;
  type: ScenarioType;
  parameters: ScenarioParameter[];
}
