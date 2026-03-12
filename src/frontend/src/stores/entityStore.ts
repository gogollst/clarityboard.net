import { create } from 'zustand';
import { getStoredSelectedEntityId, storeSelectedEntityId } from '@/lib/api';

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
  selectedEntityId: getStoredSelectedEntityId(),
  setEntities: (entities) => set({ entities }),
  setSelectedEntity: (entityId) => {
    storeSelectedEntityId(entityId);
    set({ selectedEntityId: entityId });
  },
}));
