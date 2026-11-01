from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from typing import List

from database import get_db
import models
import schemas

router = APIRouter(prefix="/wafers", tags=["Wafer Digital Twin"])

@router.post("/", response_model=schemas.WaferResponse)
def record_wafer_state(wafer_in: schemas.WaferCreate, db: Session = Depends(get_db)):
    """Synchronize an authoritative wafer state snapshot from the VR simulation."""
    record = models.WaferRecord(
        session_id=wafer_in.session_id,
        wafer_id=wafer_in.wafer_id,
        state=wafer_in.state,
        revision=wafer_in.revision,
        diameter=wafer_in.diameter,
        thickness=wafer_in.thickness,
        substrate_material=wafer_in.substrate_material,
        crystal_orientation=wafer_in.crystal_orientation,
        surface_state=wafer_in.surface_state,
        defect_count=wafer_in.defect_count
    )
    db.add(record)
    db.commit()
    db.refresh(record)
    return record

@router.get("/", response_model=List[schemas.WaferResponse])
def get_wafers(skip: int = 0, limit: int = 50, db: Session = Depends(get_db)):
    """Retrieve recent wafer snapshots."""
    return db.query(models.WaferRecord).order_by(models.WaferRecord.timestamp.desc()).offset(skip).limit(limit).all()

@router.get("/{wafer_id}/history", response_model=List[schemas.WaferResponse])
def get_wafer_history(wafer_id: str, db: Session = Depends(get_db)):
    """Retrieve full revision history and state transitions for a given wafer ID."""
    history = db.query(models.WaferRecord).filter(
        models.WaferRecord.wafer_id == wafer_id
    ).order_by(models.WaferRecord.revision.asc()).all()
    if not history:
        raise HTTPException(status_code=404, detail=f"No history found for wafer {wafer_id}")
    return history
