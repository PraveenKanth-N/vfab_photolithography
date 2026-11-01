from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from datetime import datetime
from typing import List

from database import get_db
import models
import schemas

router = APIRouter(prefix="/sessions", tags=["Training Sessions"])

@router.post("/", response_model=schemas.SessionResponse)
def create_session(session_in: schemas.SessionCreate, db: Session = Depends(get_db)):
    """Initialize a new cleanroom VR training session."""
    db_session = models.TrainingSession(
        student_id=session_in.student_id,
        module_name=session_in.module_name,
        status="ACTIVE",
        start_time=datetime.utcnow()
    )
    db.add(db_session)
    db.commit()
    db.refresh(db_session)
    return schemas.SessionResponse(
        session_id=db_session.id,
        student_id=db_session.student_id,
        module_name=db_session.module_name,
        status=db_session.status,
        start_time=db_session.start_time,
        end_time=db_session.end_time,
        final_yield=db_session.final_yield
    )

@router.get("/", response_model=List[schemas.SessionResponse])
def list_sessions(skip: int = 0, limit: int = 50, db: Session = Depends(get_db)):
    """List recent training sessions."""
    sessions = db.query(models.TrainingSession).order_by(models.TrainingSession.start_time.desc()).offset(skip).limit(limit).all()
    return [
        schemas.SessionResponse(
            session_id=s.id,
            student_id=s.student_id,
            module_name=s.module_name,
            status=s.status,
            start_time=s.start_time,
            end_time=s.end_time,
            final_yield=s.final_yield
        )
        for s in sessions
    ]

@router.get("/{session_id}", response_model=schemas.SessionResponse)
def get_session(session_id: str, db: Session = Depends(get_db)):
    """Get training session details by ID."""
    s = db.query(models.TrainingSession).filter(models.TrainingSession.id == session_id).first()
    if not s:
        raise HTTPException(status_code=404, detail="Session not found")
    return schemas.SessionResponse(
        session_id=s.id,
        student_id=s.student_id,
        module_name=s.module_name,
        status=s.status,
        start_time=s.start_time,
        end_time=s.end_time,
        final_yield=s.final_yield
    )

@router.post("/{session_id}/complete", response_model=schemas.SessionResponse)
def complete_session(session_id: str, completion: schemas.SessionComplete, db: Session = Depends(get_db)):
    """Close an active session and record the calculated wafer batch yield."""
    s = db.query(models.TrainingSession).filter(models.TrainingSession.id == session_id).first()
    if not s:
        raise HTTPException(status_code=404, detail="Session not found")
    s.status = "COMPLETED"
    s.end_time = datetime.utcnow()
    s.final_yield = completion.yield_percentage
    db.commit()
    db.refresh(s)
    return schemas.SessionResponse(
        session_id=s.id,
        student_id=s.student_id,
        module_name=s.module_name,
        status=s.status,
        start_time=s.start_time,
        end_time=s.end_time,
        final_yield=s.final_yield
    )
