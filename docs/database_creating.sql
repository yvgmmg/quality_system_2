-- DROP SCHEMA public;

CREATE SCHEMA public AUTHORIZATION postgres;

-- DROP TYPE public.frame_result;

CREATE TYPE public.frame_result AS ENUM (
	'ok',
	'for_rework',
	'defective');

-- DROP TYPE public.inspection_result;

CREATE TYPE public.inspection_result AS ENUM (
	'in progress',
	'ok',
	'normal',
	'repair');

-- DROP TYPE public.measurement_unit;

CREATE TYPE public.measurement_unit AS ENUM (
	'мм',
	'см',
	'м',
	'г',
	'кг',
	'т',
	'°C',
	'°',
	'%',
	'шт',
	'Н',
	'МПа',
	'В',
	'А',
	'м/с',
	'м²',
	'м³',
	'безразм.');

-- DROP TYPE public."role";

CREATE TYPE public."role" AS ENUM (
	'admin',
	'operator',
	'equipment specialist',
	'quality control officer');

-- DROP TYPE public.severity;

CREATE TYPE public.severity AS ENUM (
	'warning',
	'critical');

-- DROP TYPE public."source";

CREATE TYPE public."source" AS ENUM (
	'equipment',
	'frame');

-- DROP TYPE public.test_result;

CREATE TYPE public.test_result AS ENUM (
	'ok',
	'defective');

-- DROP SEQUENCE public.access_rights_access_right_id_seq;

CREATE SEQUENCE public.access_rights_access_right_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.batch_batch_id_seq;

CREATE SEQUENCE public.batch_batch_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.camera_camera_id_seq;

CREATE SEQUENCE public.camera_camera_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.camera_frame_camera_frame_id_seq;

CREATE SEQUENCE public.camera_frame_camera_frame_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.equipment_inspection_form_equipment_inspection_form_id_seq;

CREATE SEQUENCE public.equipment_inspection_form_equipment_inspection_form_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.equipment_inspection_form_par_equipment_inspection_form_par_seq;

CREATE SEQUENCE public.equipment_inspection_form_par_equipment_inspection_form_par_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.equipment_inspection_form_pro_equipment_inspection_form_pro_seq;

CREATE SEQUENCE public.equipment_inspection_form_pro_equipment_inspection_form_pro_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.frame_frame_id_seq;

CREATE SEQUENCE public.frame_frame_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.frame_test_form_frame_frame_test_form_frame_id_seq;

CREATE SEQUENCE public.frame_test_form_frame_frame_test_form_frame_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.frame_test_form_frame_test_form_id_seq;

CREATE SEQUENCE public.frame_test_form_frame_test_form_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.frame_test_form_params_frame_test_form_params_id_seq;

CREATE SEQUENCE public.frame_test_form_params_frame_test_form_params_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.instruction_instruction_id_seq;

CREATE SEQUENCE public.instruction_instruction_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.instruction_production_equipment_instruction_production_equipme;

CREATE SEQUENCE public.instruction_production_equipment_instruction_production_equipme
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.instruction_production_order_instruction_production_order_i_seq;

CREATE SEQUENCE public.instruction_production_order_instruction_production_order_i_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.material_material_id_seq;

CREATE SEQUENCE public.material_material_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.notification_notification_id_seq;

CREATE SEQUENCE public.notification_notification_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.params_params_id_seq;

CREATE SEQUENCE public.params_params_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.production_equipment_production_equipment_id_seq;

CREATE SEQUENCE public.production_equipment_production_equipment_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.production_order_production_order_id_seq;

CREATE SEQUENCE public.production_order_production_order_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.regilatory_information_instru_regilatory_information_instru_seq;

CREATE SEQUENCE public.regilatory_information_instru_regilatory_information_instru_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.regulatory_information_regulatory_information_id_seq;

CREATE SEQUENCE public.regulatory_information_regulatory_information_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.regulatory_infromation_produc_regulatory_infromation_produc_seq;

CREATE SEQUENCE public.regulatory_infromation_produc_regulatory_infromation_produc_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.requisition_invoice_requisition_invoice_id_seq;

CREATE SEQUENCE public.requisition_invoice_requisition_invoice_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.sensor_readings_sensor_readings_id_seq;

CREATE SEQUENCE public.sensor_readings_sensor_readings_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.sensor_sensor_id_seq;

CREATE SEQUENCE public.sensor_sensor_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.user_profile_access_rights_user_profile_access_rights_id_seq;

CREATE SEQUENCE public.user_profile_access_rights_user_profile_access_rights_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.user_profile_user_profile_id_seq;

CREATE SEQUENCE public.user_profile_user_profile_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.workshop_production_order_workshop_production_order_seq;

CREATE SEQUENCE public.workshop_production_order_workshop_production_order_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.workshop_workshop_id_seq;

CREATE SEQUENCE public.workshop_workshop_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 2147483647
	START 1
	CACHE 1
	NO CYCLE;-- public.access_rights определение

-- Drop table

-- DROP TABLE public.access_rights;

CREATE TABLE public.access_rights (
	access_right_id serial4 NOT NULL,
	access_right varchar NULL,
	CONSTRAINT access_rights_pk PRIMARY KEY (access_right_id)
);


-- public.instruction определение

-- Drop table

-- DROP TABLE public.instruction;

CREATE TABLE public.instruction (
	"name" varchar(255) NOT NULL,
	"content" text NOT NULL,
	path_to_templates varchar(500) NOT NULL,
	instruction_id serial4 NOT NULL,
	CONSTRAINT instruction_pk PRIMARY KEY (instruction_id)
);


-- public.params определение

-- Drop table

-- DROP TABLE public.params;

CREATE TABLE public.params (
	params_id serial4 NOT NULL,
	value numeric NULL,
	passed bool NULL,
	CONSTRAINT params_pk PRIMARY KEY (params_id)
);


-- public.production_order определение

-- Drop table

-- DROP TABLE public.production_order;

CREATE TABLE public.production_order (
	production_order_id serial4 NOT NULL,
	description text NULL,
	start_date date NOT NULL,
	end_date date NULL,
	CONSTRAINT chk_production_order_dates CHECK (((end_date IS NULL) OR (end_date > start_date))),
	CONSTRAINT production_order_pk PRIMARY KEY (production_order_id)
);


-- public.regulatory_information определение

-- Drop table

-- DROP TABLE public.regulatory_information;

CREATE TABLE public.regulatory_information (
	regulatory_information_id serial4 NOT NULL,
	"name" varchar(255) NOT NULL,
	"type" public."source" NOT NULL,
	description text NULL,
	min_value float4 NULL, -- Минимально допустимое значение (не менее)
	max_value float4 NULL, -- Максимально допустимое значение (не более)
	approval_date date NULL,
	end_date date NULL,
	measurement public.measurement_unit NOT NULL,
	CONSTRAINT chk_regulatory_information_at_least_one_value CHECK (((min_value IS NOT NULL) OR (max_value IS NOT NULL))),
	CONSTRAINT chk_regulatory_information_dates CHECK (((end_date IS NULL) OR (approval_date IS NULL) OR (end_date > approval_date))),
	CONSTRAINT chk_regulatory_information_range CHECK (((min_value IS NULL) OR (max_value IS NULL) OR (max_value > min_value))),
	CONSTRAINT regulatory_information_pk PRIMARY KEY (regulatory_information_id)
);

-- Column comments

COMMENT ON COLUMN public.regulatory_information.min_value IS 'Минимально допустимое значение (не менее)';
COMMENT ON COLUMN public.regulatory_information.max_value IS 'Максимально допустимое значение (не более)';


-- public.workshop определение

-- Drop table

-- DROP TABLE public.workshop;

CREATE TABLE public.workshop (
	workshop_id serial4 NOT NULL,
	"number" int4 NOT NULL,
	appointment varchar(255) NOT NULL,
	CONSTRAINT workshop_pk PRIMARY KEY (workshop_id)
);


-- public.batch определение

-- Drop table

-- DROP TABLE public.batch;

CREATE TABLE public.batch (
	batch_id serial4 NOT NULL,
	"number" int4 NOT NULL,
	production_date date NOT NULL,
	production_order_id int4 NULL,
	CONSTRAINT batch_pk PRIMARY KEY (batch_id),
	CONSTRAINT batch_production_order_fk FOREIGN KEY (production_order_id) REFERENCES public.production_order(production_order_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.camera определение

-- Drop table

-- DROP TABLE public.camera;

CREATE TABLE public.camera (
	inventory_number varchar(14) NULL,
	"name" varchar(255) NULL,
	workshop_id int4 NULL,
	camera_id serial4 NOT NULL,
	CONSTRAINT camera_pk PRIMARY KEY (camera_id),
	CONSTRAINT chk_camera_inventory_number_okof CHECK (((inventory_number IS NULL) OR ((inventory_number)::text ~ '^\d{3}\.\d{2}\.\d{2}\.\d{2}\.\d{3}$'::text))),
	CONSTRAINT camera_workshop_fk FOREIGN KEY (workshop_id) REFERENCES public.workshop(workshop_id)
);


-- public.frame определение

-- Drop table

-- DROP TABLE public.frame;

CREATE TABLE public.frame (
	frame_id serial4 NOT NULL,
	production_number varchar(255) NOT NULL,
	"result" public.frame_result NULL,
	instruction_id int4 NULL,
	batch_id int4 NULL,
	workshop_id int4 NULL,
	CONSTRAINT chk_frame_production_number_format CHECK (((production_number)::text ~ '^[А-ЯЁа-яёA-Za-z]{2}-\d{4}-\d{5}$'::text)),
	CONSTRAINT frame_pk PRIMARY KEY (frame_id),
	CONSTRAINT frame_batch_fk FOREIGN KEY (batch_id) REFERENCES public.batch(batch_id),
	CONSTRAINT frame_instruction_fk FOREIGN KEY (instruction_id) REFERENCES public.instruction(instruction_id),
	CONSTRAINT frame_workshop_fk FOREIGN KEY (workshop_id) REFERENCES public.workshop(workshop_id)
);


-- public.instruction_production_order определение

-- Drop table

-- DROP TABLE public.instruction_production_order;

CREATE TABLE public.instruction_production_order (
	instruction_production_order_id int4 DEFAULT nextval('instruction_production_order_instruction_production_order_i_seq'::regclass) NOT NULL,
	production_order_id int4 NULL,
	instruction_id int4 NULL,
	CONSTRAINT instruction_production_order_pk PRIMARY KEY (instruction_production_order_id),
	CONSTRAINT instruction_production_order_instruction_fk FOREIGN KEY (instruction_id) REFERENCES public.instruction(instruction_id),
	CONSTRAINT instruction_production_order_production_order_fk FOREIGN KEY (production_order_id) REFERENCES public.production_order(production_order_id)
);


-- public.production_equipment определение

-- Drop table

-- DROP TABLE public.production_equipment;

CREATE TABLE public.production_equipment (
	"name" varchar(255) NOT NULL,
	serial_number varchar(100) NULL,
	inventory_number varchar(20) NULL,
	workshop_id int4 NULL,
	production_equipment_id serial4 NOT NULL,
	CONSTRAINT chk_production_equipment_inventory_number_okof CHECK (((inventory_number IS NULL) OR ((inventory_number)::text ~ '^\d{3}\.\d{2}\.\d{2}\.\d{2}\.\d{3}$'::text))),
	CONSTRAINT production_equipment_pk PRIMARY KEY (production_equipment_id),
	CONSTRAINT production_equipment_workshop_fk FOREIGN KEY (workshop_id) REFERENCES public.workshop(workshop_id)
);


-- public.regilatory_information_instruction определение

-- Drop table

-- DROP TABLE public.regilatory_information_instruction;

CREATE TABLE public.regilatory_information_instruction (
	regulatory_information_instruction_id int4 DEFAULT nextval('regilatory_information_instru_regilatory_information_instru_seq'::regclass) NOT NULL,
	regulatory_information_id int4 NULL,
	instruction_id int4 NULL,
	CONSTRAINT regilatory_information_instruction_pk PRIMARY KEY (regulatory_information_instruction_id),
	CONSTRAINT regilatory_information_instruction_instruction_fk FOREIGN KEY (instruction_id) REFERENCES public.instruction(instruction_id),
	CONSTRAINT regilatory_information_instruction_regulatory_information_fk FOREIGN KEY (regulatory_information_id) REFERENCES public.regulatory_information(regulatory_information_id)
);


-- public.regulatory_infromation_production_equipment определение

-- Drop table

-- DROP TABLE public.regulatory_infromation_production_equipment;

CREATE TABLE public.regulatory_infromation_production_equipment (
	regulatory_information_production_equipment_id int4 DEFAULT nextval('regulatory_infromation_produc_regulatory_infromation_produc_seq'::regclass) NOT NULL,
	production_equipment_id int4 NULL,
	regulatory_information_id int4 NULL,
	CONSTRAINT regulatory_infromation_production_equipment_pk PRIMARY KEY (regulatory_information_production_equipment_id),
	CONSTRAINT regulatory_infromation_production_equipment_production_equipmen FOREIGN KEY (production_equipment_id) REFERENCES public.production_equipment(production_equipment_id),
	CONSTRAINT regulatory_infromation_production_equipment_regulatory_informat FOREIGN KEY (regulatory_information_id) REFERENCES public.regulatory_information(regulatory_information_id)
);


-- public."requisition-invoice" определение

-- Drop table

-- DROP TABLE public."requisition-invoice";

CREATE TABLE public."requisition-invoice" (
	"requisition-invoice_id" int4 DEFAULT nextval('frame_test_form_params_frame_test_form_params_id_seq'::regclass) NOT NULL,
	production_order_id int4 NULL,
	creation_date date NOT NULL,
	CONSTRAINT requisition_invoice_pk PRIMARY KEY ("requisition-invoice_id"),
	CONSTRAINT requisition_invoice_production_order_fk FOREIGN KEY (production_order_id) REFERENCES public.production_order(production_order_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.sensor определение

-- Drop table

-- DROP TABLE public.sensor;

CREATE TABLE public.sensor (
	sensor_id serial4 NOT NULL,
	inventory_number varchar(14) NOT NULL,
	"name" varchar(255) NOT NULL,
	mesurement varchar(20) NOT NULL,
	workshop_id int4 NULL,
	sensor_type_id varchar NULL,
	CONSTRAINT chk_sensor_inventory_number_okof CHECK (((inventory_number IS NULL) OR ((inventory_number)::text ~ '^\d{3}\.\d{2}\.\d{2}\.\d{2}\.\d{3}$'::text))),
	CONSTRAINT sensor_pk PRIMARY KEY (sensor_id),
	CONSTRAINT sensor_workshop_fk FOREIGN KEY (workshop_id) REFERENCES public.workshop(workshop_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.sensor_readings определение

-- Drop table

-- DROP TABLE public.sensor_readings;

CREATE TABLE public.sensor_readings (
	sensor_readings_id serial4 NOT NULL,
	sensor_id int4 NULL,
	value float8 NOT NULL,
	readings_time timestamp NOT NULL,
	frame_id int4 NULL,
	production_equipment_id int4 NULL,
	measurement public.measurement_unit NOT NULL,
	CONSTRAINT chk_sensor_readings_one_source CHECK (((((frame_id IS NOT NULL))::integer + ((production_equipment_id IS NOT NULL))::integer) = 1)),
	CONSTRAINT sensor_readings_pk PRIMARY KEY (sensor_readings_id),
	CONSTRAINT sensor_readings_frame_fk FOREIGN KEY (frame_id) REFERENCES public.frame(frame_id),
	CONSTRAINT sensor_readings_production_equipment_fk FOREIGN KEY (production_equipment_id) REFERENCES public.production_equipment(production_equipment_id) ON DELETE SET NULL ON UPDATE CASCADE,
	CONSTRAINT sensor_readings_sensor_fk FOREIGN KEY (sensor_id) REFERENCES public.sensor(sensor_id)
);


-- public.user_profile определение

-- Drop table

-- DROP TABLE public.user_profile;

CREATE TABLE public.user_profile (
	"name" varchar(255) NOT NULL,
	surname varchar(255) NOT NULL,
	patron varchar(255) NULL,
	workshop_id int4 NOT NULL,
	password_hash varchar(255) NOT NULL,
	personnel_number varchar NULL,
	user_profile_id serial4 NOT NULL,
	"role" public."role" NOT NULL,
	CONSTRAINT chk_name_format CHECK (((name)::text ~ '^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$'::text)),
	CONSTRAINT chk_password_hash_min_length CHECK ((length((password_hash)::text) >= 60)),
	CONSTRAINT chk_patron_format CHECK (((patron IS NULL) OR ((patron)::text ~ '^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$'::text))),
	CONSTRAINT chk_personnel_number_format CHECK (((personnel_number IS NULL) OR ((personnel_number)::text ~ '^[А-ЯЁа-яёA-Za-z]\d+$'::text))),
	CONSTRAINT chk_surname_format CHECK (((surname)::text ~ '^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$'::text)),
	CONSTRAINT user_profile_pk PRIMARY KEY (user_profile_id),
	CONSTRAINT user_profile_workshop_fk FOREIGN KEY (workshop_id) REFERENCES public.workshop(workshop_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.user_profile_access_rights определение

-- Drop table

-- DROP TABLE public.user_profile_access_rights;

CREATE TABLE public.user_profile_access_rights (
	user_profile_access_rights_id serial4 NOT NULL,
	user_profile_id int4 NOT NULL,
	access_right_id int4 NULL,
	CONSTRAINT user_profile_access_rights_pk PRIMARY KEY (user_profile_access_rights_id),
	CONSTRAINT user_profile_access_rights_access_rights_fk FOREIGN KEY (access_right_id) REFERENCES public.access_rights(access_right_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT user_profile_access_rights_user_profile_fk FOREIGN KEY (user_profile_id) REFERENCES public.user_profile(user_profile_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.workshop_production_order определение

-- Drop table

-- DROP TABLE public.workshop_production_order;

CREATE TABLE public.workshop_production_order (
	workshop_production_order serial4 NOT NULL,
	workshop_id int4 NULL,
	production_order_id int4 NULL,
	CONSTRAINT workshop_production_order_pk PRIMARY KEY (workshop_production_order),
	CONSTRAINT workshop_production_order_production_order_fk FOREIGN KEY (production_order_id) REFERENCES public.production_order(production_order_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT workshop_production_order_workshop_fk FOREIGN KEY (workshop_id) REFERENCES public.workshop(workshop_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.camera_frame определение

-- Drop table

-- DROP TABLE public.camera_frame;

CREATE TABLE public.camera_frame (
	camera_frame_id serial4 NOT NULL,
	path_to_camera_frame varchar(500) NOT NULL,
	path_to_processed_camera_frame varchar(500) NOT NULL,
	record_time timestamp NOT NULL,
	camera_id int4 NULL,
	frame_id int4 NULL,
	CONSTRAINT camera_frame_pk PRIMARY KEY (camera_frame_id),
	CONSTRAINT camera_frame_camera_fk FOREIGN KEY (camera_id) REFERENCES public.camera(camera_id),
	CONSTRAINT camera_frame_frame_fk FOREIGN KEY (frame_id) REFERENCES public.frame(frame_id)
);


-- public.equipment_inspection_form определение

-- Drop table

-- DROP TABLE public.equipment_inspection_form;

CREATE TABLE public.equipment_inspection_form (
	equipment_inspection_form_id serial4 NOT NULL,
	"name" varchar(255) NOT NULL,
	creation_date date NOT NULL,
	complite_date date NULL,
	user_profile_id int4 NULL,
	"result" public.inspection_result NULL,
	CONSTRAINT equipment_inspection_form_pk PRIMARY KEY (equipment_inspection_form_id),
	CONSTRAINT equipment_inspection_form_user_profile_fk FOREIGN KEY (user_profile_id) REFERENCES public.user_profile(user_profile_id)
);


-- public.equipment_inspection_form_params определение

-- Drop table

-- DROP TABLE public.equipment_inspection_form_params;

CREATE TABLE public.equipment_inspection_form_params (
	equipment_inspection_form_params_id int4 DEFAULT nextval('equipment_inspection_form_par_equipment_inspection_form_par_seq'::regclass) NOT NULL,
	equipment_inspection_form_id int4 NULL,
	params_id int4 NULL,
	CONSTRAINT equipment_inspection_form_params_pk PRIMARY KEY (equipment_inspection_form_params_id),
	CONSTRAINT equipment_inspection_form_params_equipment_inspection_form_fk FOREIGN KEY (equipment_inspection_form_id) REFERENCES public.equipment_inspection_form(equipment_inspection_form_id),
	CONSTRAINT equipment_inspection_form_params_params_fk FOREIGN KEY (params_id) REFERENCES public.params(params_id)
);


-- public.equipment_inspection_form_production_equipment определение

-- Drop table

-- DROP TABLE public.equipment_inspection_form_production_equipment;

CREATE TABLE public.equipment_inspection_form_production_equipment (
	equipment_inspection_form_production_equipment int4 DEFAULT nextval('equipment_inspection_form_pro_equipment_inspection_form_pro_seq'::regclass) NOT NULL,
	equipment_inspection_form_id int4 NULL,
	production_equipment_id int4 NULL,
	CONSTRAINT equipment_inspection_form_production_equipment_pk PRIMARY KEY (equipment_inspection_form_production_equipment),
	CONSTRAINT equipment_inspection_form_production_equipment_equipment_inspec FOREIGN KEY (equipment_inspection_form_id) REFERENCES public.equipment_inspection_form(equipment_inspection_form_id),
	CONSTRAINT equipment_inspection_form_production_equipment_production_equip FOREIGN KEY (production_equipment_id) REFERENCES public.production_equipment(production_equipment_id)
);


-- public.frame_test_form определение

-- Drop table

-- DROP TABLE public.frame_test_form;

CREATE TABLE public.frame_test_form (
	frame_test_form_id serial4 NOT NULL,
	"name" varchar(255) NOT NULL,
	"result" public.test_result NOT NULL,
	"comments" text NULL,
	user_profile_id int4 NULL,
	CONSTRAINT frame_test_form_pk PRIMARY KEY (frame_test_form_id),
	CONSTRAINT frame_test_form_user_profile_fk FOREIGN KEY (user_profile_id) REFERENCES public.user_profile(user_profile_id)
);


-- public.frame_test_form_frame определение

-- Drop table

-- DROP TABLE public.frame_test_form_frame;

CREATE TABLE public.frame_test_form_frame (
	frame_test_form_frame_id serial4 NOT NULL,
	frame_id int4 NULL,
	frame_test_form_id int4 NULL,
	CONSTRAINT frame_test_form_frame_pk PRIMARY KEY (frame_test_form_frame_id),
	CONSTRAINT frame_test_form_frame_frame_fk FOREIGN KEY (frame_id) REFERENCES public.frame(frame_id),
	CONSTRAINT frame_test_form_frame_frame_test_form_fk FOREIGN KEY (frame_test_form_id) REFERENCES public.frame_test_form(frame_test_form_id)
);


-- public.frame_test_form_params определение

-- Drop table

-- DROP TABLE public.frame_test_form_params;

CREATE TABLE public.frame_test_form_params (
	frame_test_form_id int4 NULL,
	params_id int4 NULL,
	frame_test_form_params_id serial4 NOT NULL,
	CONSTRAINT frame_test_form_params_pk PRIMARY KEY (frame_test_form_params_id),
	CONSTRAINT frame_test_form_params_frame_test_form_fk FOREIGN KEY (frame_test_form_id) REFERENCES public.frame_test_form(frame_test_form_id),
	CONSTRAINT frame_test_form_params_params_fk FOREIGN KEY (params_id) REFERENCES public.params(params_id)
);


-- public.instruction_production_equipment определение

-- Drop table

-- DROP TABLE public.instruction_production_equipment;

CREATE TABLE public.instruction_production_equipment (
	instruction_production_equipment_id int4 DEFAULT nextval('instruction_production_equipment_instruction_production_equipme'::regclass) NOT NULL,
	production_equipment_id int4 NULL,
	instruction_id int4 NULL,
	CONSTRAINT instruction_production_equipment_pk PRIMARY KEY (instruction_production_equipment_id),
	CONSTRAINT instruction_production_equipment_instruction_fk FOREIGN KEY (instruction_id) REFERENCES public.instruction(instruction_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT instruction_production_equipment_production_equipment_fk FOREIGN KEY (production_equipment_id) REFERENCES public.production_equipment(production_equipment_id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- public.material определение

-- Drop table

-- DROP TABLE public.material;

CREATE TABLE public.material (
	"requisition-invoice_id" int4 NULL,
	account varchar(255) NOT NULL,
	"name" varchar(255) NOT NULL,
	measurement public.measurement_unit NOT NULL,
	count int4 NOT NULL,
	price money NOT NULL,
	summ money NOT NULL,
	material_id serial4 NOT NULL,
	CONSTRAINT material_pk PRIMARY KEY (material_id),
	CONSTRAINT material_requisition_invoice_fk FOREIGN KEY ("requisition-invoice_id") REFERENCES public."requisition-invoice"("requisition-invoice_id")
);

-- Table Triggers

create trigger trg_material_summ_before_insert_update before
insert
    or
update
    of price,
    count on
    public.material for each row execute function trg_material_calc_summ();


-- public.notification определение

-- Drop table

-- DROP TABLE public.notification;

CREATE TABLE public.notification (
	notification_id serial4 NOT NULL,
	"name" varchar(255) NOT NULL,
	"source" public."source" NOT NULL,
	severity public.severity NOT NULL,
	description varchar(255) NULL,
	user_profile_id int4 NULL,
	frame_id int4 NULL,
	production_equipment_id int4 NULL,
	notification_time timestamp NOT NULL,
	CONSTRAINT chk_notification_one_source CHECK (((((frame_id IS NOT NULL))::integer + ((production_equipment_id IS NOT NULL))::integer) = 1)),
	CONSTRAINT notification_pk PRIMARY KEY (notification_id),
	CONSTRAINT notification_frame_fk FOREIGN KEY (frame_id) REFERENCES public.frame(frame_id) ON DELETE SET NULL ON UPDATE CASCADE,
	CONSTRAINT notification_production_equipment_fk FOREIGN KEY (production_equipment_id) REFERENCES public.production_equipment(production_equipment_id) ON DELETE SET NULL ON UPDATE CASCADE,
	CONSTRAINT notification_user_profile_fk FOREIGN KEY (user_profile_id) REFERENCES public.user_profile(user_profile_id)
);



-- DROP FUNCTION public.trg_material_calc_summ();

CREATE OR REPLACE FUNCTION public.trg_material_calc_summ()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Приводим money к numeric для умножения, затем обратно в money
    NEW.summ := (NEW.price::numeric * NEW.count)::money;
    RETURN NEW;
END;
$function$
;