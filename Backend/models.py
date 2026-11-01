"""
SQLAlchemy ORM models for Semiconductor Digital Twin platform.
"""

from sqlalchemy import Column, String, Float, Integer, Boolean, DateTime, Text, ForeignKey
from sqlalchemy.orm import relationship
from datetime import datetime
import uuid

from database import Base

def generate_uuid():
    return str(uuid.uuid4())

class TrainingSession(Base):
    __tablename__ = "training_sessions"

    id = Column(String(36), primary_key=True, default=generate_uuid)
    student_id = Column(String(64), nullable=False, index=True)
    module_name = Column(String(128), nullable=False)
    status = Column(String(32), default="ACTIVE")  # ACTIVE, COMPLETED, ABORTED
    start_time = Column(DateTime, default=datetime.utcnow)
    end_time = Column(DateTime, nullable=True)
    final_yield = Column(Float, default=100.0)

    # Relationships
    wafers = relationship("WaferRecord", back_populates="session", cascade="all, delete-orphan")
    telemetry = relationship("EquipmentTelemetry", back_populates="session", cascade="all, delete-orphan")
    defects = relationship("DefectRecord", back_populates="session", cascade="all, delete-orphan")


class WaferRecord(Base):
    __tablename__ = "wafer_records"

    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String(36), ForeignKey("training_sessions.id"), nullable=True)
    wafer_id = Column(String(64), nullable=False, index=True)
    state = Column(String(64), nullable=False)
    revision = Column(Integer, default=0)
    diameter = Column(Float, default=100.0)
    thickness = Column(Float, default=0.5)
    substrate_material = Column(String(64), default="Silicon")
    crystal_orientation = Column(String(32), default="<100>")
    surface_state = Column(String(64), default="Clean")
    defect_count = Column(Integer, default=0)
    timestamp = Column(DateTime, default=datetime.utcnow)

    session = relationship("TrainingSession", back_populates="wafers")


class EquipmentTelemetry(Base):
    __tablename__ = "equipment_telemetry"

    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String(36), ForeignKey("training_sessions.id"), nullable=True)
    equipment_name = Column(String(64), nullable=False, index=True)
    parameter_name = Column(String(64), nullable=False)
    parameter_value = Column(Float, nullable=False)
    unit = Column(String(32), default="")
    timestamp = Column(DateTime, default=datetime.utcnow)

    session = relationship("TrainingSession", back_populates="telemetry")


class DefectRecord(Base):
    __tablename__ = "defect_records"

    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String(36), ForeignKey("training_sessions.id"), nullable=True)
    wafer_id = Column(String(64), nullable=False, index=True)
    defect_id = Column(String(64), nullable=False)
    defect_type = Column(String(128), nullable=False)
    severity = Column(Float, default=0.0)
    is_critical = Column(Boolean, default=False)
    origin_equipment = Column(String(64), default="Unknown")
    pos_x = Column(Float, default=0.0)
    pos_y = Column(Float, default=0.0)
    pos_z = Column(Float, default=0.0)
    timestamp = Column(DateTime, default=datetime.utcnow)

    session = relationship("TrainingSession", back_populates="defects")


class RecipeRecord(Base):
    __tablename__ = "recipes"

    id = Column(String(64), primary_key=True)
    process_type = Column(String(64), nullable=False, index=True) # e.g. "SpinCoater", "Hotplate", "Exposure", "PlasmaEtch"
    recipe_name = Column(String(128), nullable=False)
    description = Column(Text, default="")
    parameters_json = Column(Text, nullable=False) # Serialized JSON dict of parameters
    created_at = Column(DateTime, default=datetime.utcnow)


class SocraticAssessment(Base):
    __tablename__ = "socratic_assessments"

    id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String(36), nullable=True)
    student_id = Column(String(64), nullable=False, index=True)
    concept_id = Column(String(128), nullable=False)
    prior_mastery = Column(Float, default=0.5)
    posterior_mastery = Column(Float, default=0.5)
    response_correct = Column(Boolean, nullable=False)
    timestamp = Column(DateTime, default=datetime.utcnow)
