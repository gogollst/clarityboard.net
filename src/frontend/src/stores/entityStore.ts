import { create } from 'zustand';

export interface LegalEntity {
  id: string;
  name: string;
  legalForm: string;
  taxId: string;
  currency: string;
}

interface EntityState {
  entities: LegalEntity[];
  selectedEntityId: string | null;
  setEntities: (entities: LegalEntity[]) => void;
  setSelectedEntity: (entityId: string) => void;
}

export const useEntityStore = create<EntityState>((set) => ({
  entities: [],
  selectedEntityId: null,
  setEntities: (entities) => set({ entities }),
  setSelectedEntity: (entityId) => set({ selectedEntityId: entityId }),
}));
