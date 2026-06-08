CREATE TABLE IF NOT EXISTS production_equipment_frame (
    production_equipment_id integer NOT NULL,
    frame_id integer NOT NULL,
    CONSTRAINT production_equipment_frame_pk
        PRIMARY KEY (production_equipment_id, frame_id),
    CONSTRAINT production_equipment_frame_equipment_fk
        FOREIGN KEY (production_equipment_id)
        REFERENCES production_equipment (production_equipment_id)
        ON DELETE CASCADE,
    CONSTRAINT production_equipment_frame_frame_fk
        FOREIGN KEY (frame_id)
        REFERENCES frame (frame_id)
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS production_equipment_frame_unique
    ON production_equipment_frame (production_equipment_id, frame_id);

ALTER TABLE sensor
    ADD COLUMN IF NOT EXISTS production_equipment_id integer NULL;

ALTER TABLE sensor
    DROP CONSTRAINT IF EXISTS sensor_production_equipment_fk;

ALTER TABLE sensor
    ADD CONSTRAINT sensor_production_equipment_fk
        FOREIGN KEY (production_equipment_id)
        REFERENCES production_equipment (production_equipment_id)
        ON DELETE SET NULL;

UPDATE sensor
SET production_equipment_id = NULL
WHERE production_equipment_id IS NOT NULL
  AND sensor_type_id IN (
      SELECT sensor_type_id
      FROM sensor_type_classifier
      WHERE code <> '02'
  );

CREATE OR REPLACE FUNCTION enforce_equipment_sensor_code()
RETURNS trigger AS $$
BEGIN
    IF NEW.production_equipment_id IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM sensor_type_classifier stc
           WHERE stc.sensor_type_id = NEW.sensor_type_id
             AND stc.code = '02'
       ) THEN
        RAISE EXCEPTION 'Only sensors with code 02 can be linked to production equipment.';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS sensor_equipment_code_guard ON sensor;

CREATE TRIGGER sensor_equipment_code_guard
BEFORE INSERT OR UPDATE OF production_equipment_id, sensor_type_id
ON sensor
FOR EACH ROW
EXECUTE FUNCTION enforce_equipment_sensor_code();

ALTER TABLE check_notification
    ADD COLUMN IF NOT EXISTS checked_at timestamp without time zone NULL;

ALTER TABLE check_notification
    DROP CONSTRAINT IF EXISTS check_notification_frame_fk;

ALTER TABLE check_notification
    DROP COLUMN IF EXISTS frame_id;
