from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session
from typing import List, Optional

from database import get_db
import models
import schemas

router = APIRouter(prefix="/telemetry", tags=["Equipment Telemetry"])

@router.post("/", response_model=dict)
def log_telemetry(telemetry_in: schemas.TelemetryCreate, db: Session = Depends(get_db)):
    """Ingest operational sensor metric from cleanroom equipment."""
    record = models.EquipmentTelemetry(
        session_id=telemetry_in.session_id,
        equipment_name=telemetry_in.equipment_name,
        parameter_name=telemetry_in.parameter_name,
        parameter_value=telemetry_in.parameter_value,
        unit=telemetry_in.unit
    )
    db.add(record)
    db.commit()
    return {"status": "success", "id": record.id}

@router.get("/latest")
def get_latest_telemetry(equipment: Optional[str] = None, db: Session = Depends(get_db)):
    """Retrieve the latest operational parameter values for active equipment."""
    query = db.query(models.EquipmentTelemetry)
    if equipment:
        query = query.filter(models.EquipmentTelemetry.equipment_name == equipment)
    records = query.order_by(models.EquipmentTelemetry.timestamp.desc()).limit(20).all()
    return [
        {
            "equipment": r.equipment_name,
            "parameter": r.parameter_name,
            "value": r.parameter_value,
            "unit": r.unit,
            "timestamp": r.timestamp
        }
        for r in records
    ]
