-- =============================================================
--  Tour Planner – PostgreSQL Initialization Script
-- =============================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

DROP VIEW  IF EXISTS v_tour_search CASCADE; -- Kaskadiert wegen den Tabellen-Drops
DROP TABLE IF EXISTS tour_waypoint CASCADE;
DROP TABLE IF EXISTS tour_log      CASCADE;
DROP TABLE IF EXISTS tour          CASCADE;
DROP TABLE IF EXISTS app_user      CASCADE;
DROP TYPE  IF EXISTS transport_type CASCADE;

CREATE TYPE transport_type AS ENUM (
    'driving-car',
    'driving-hgv',
    'cycling-regular',
    'cycling-road',
    'foot-walking',
    'foot-hiking'
);

CREATE TABLE app_user (
    id            UUID         NOT NULL DEFAULT gen_random_uuid(),
    username      VARCHAR(50)  NOT NULL,
    email         VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at    TIMESTAMP    NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_app_user       PRIMARY KEY (id),
    CONSTRAINT uq_user_username  UNIQUE (username),
    CONSTRAINT uq_user_email     UNIQUE (email),
    CONSTRAINT ck_user_email_fmt CHECK (email LIKE '%@%')
);

CREATE TABLE tour (
    id                  UUID           NOT NULL DEFAULT gen_random_uuid(),
    user_id             UUID           NOT NULL,
    name                VARCHAR(200)   NOT NULL,
    description         TEXT,
    transport_type      transport_type NOT NULL,
    distance_km         NUMERIC(10,2),
    estimated_time_min  INTEGER,
    route_information   TEXT,
    map_image_path      VARCHAR(500),
    popularity          NUMERIC(5,2)   NOT NULL DEFAULT 0.0,
    child_friendliness  NUMERIC(5,2)   NOT NULL DEFAULT 0.0,
    created_at          TIMESTAMP      NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP      NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_tour            PRIMARY KEY (id),
    CONSTRAINT fk_tour_user       FOREIGN KEY (user_id) REFERENCES app_user(id) ON DELETE CASCADE,
    CONSTRAINT ck_tour_distance   CHECK (distance_km IS NULL OR distance_km >= 0),
    CONSTRAINT ck_tour_time       CHECK (estimated_time_min IS NULL OR estimated_time_min >= 0),
    CONSTRAINT ck_tour_popularity CHECK (popularity >= 0),
    CONSTRAINT ck_tour_child_fr   CHECK (child_friendliness >= 0 AND child_friendliness <= 10)
);

CREATE INDEX idx_tour_user_id ON tour(user_id);

CREATE TABLE tour_waypoint (
    id          UUID           NOT NULL DEFAULT gen_random_uuid(),
    tour_id     UUID           NOT NULL,
    order_index INTEGER          NOT NULL,
    label       VARCHAR(300),
    latitude    NUMERIC(10, 8) NOT NULL,
    longitude   NUMERIC(11, 8) NOT NULL,

    CONSTRAINT pk_tour_waypoint PRIMARY KEY (id),
    CONSTRAINT fk_waypoint_tour FOREIGN KEY (tour_id) REFERENCES tour(id) ON DELETE CASCADE,
    CONSTRAINT uq_tour_order    UNIQUE (tour_id, order_index)
);

CREATE INDEX idx_waypoint_tour_order ON tour_waypoint(tour_id, order_index);

CREATE TABLE tour_log (
    id                 UUID          NOT NULL DEFAULT gen_random_uuid(),
    tour_id            UUID          NOT NULL,
    date_time          TIMESTAMP     NOT NULL,
    comment            TEXT,
    difficulty         SMALLINT      NOT NULL,
    total_distance_km  NUMERIC(10,2) NOT NULL,
    total_time_min     INTEGER       NOT NULL,
    rating             SMALLINT      NOT NULL,

    CONSTRAINT pk_tour_log       PRIMARY KEY (id),
    CONSTRAINT fk_tour_log_tour  FOREIGN KEY (tour_id) REFERENCES tour(id) ON DELETE CASCADE,
    CONSTRAINT ck_log_difficulty CHECK (difficulty BETWEEN 1 AND 5),
    CONSTRAINT ck_log_rating     CHECK (rating BETWEEN 1 AND 5),
    CONSTRAINT ck_log_distance   CHECK (total_distance_km >= 0),
    CONSTRAINT ck_log_time       CHECK (total_time_min >= 0)
);

CREATE INDEX idx_tour_log_tour_id ON tour_log(tour_id);

-- Trigger für updated_at
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_tour_updated_at
    BEFORE UPDATE ON tour
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- =============================================================
--  KORRIGIERTER VIEW (w.label statt w.address & GROUP BY angepasst)
-- =============================================================
CREATE OR REPLACE VIEW v_tour_search AS
SELECT
    t.id AS tour_id,
    t.user_id,
    t.name,
    t.description,
    t.transport_type,
    t.distance_km,
    t.estimated_time_min,
    t.popularity,
    t.child_friendliness,
    COUNT(DISTINCT l.id) AS log_count,
    AVG(l.rating) AS avg_rating,
    AVG(l.difficulty) AS avg_difficulty,
    to_tsvector('english',
        coalesce(t.name,'')        || ' ' ||
        coalesce(t.description,'') || ' ' ||
        coalesce(string_agg(DISTINCT w.label, ' '),'') || ' ' || -- HIER: label statt address
        coalesce(string_agg(DISTINCT l.comment, ' '),'')
    ) AS search_vector
FROM tour t
LEFT JOIN tour_log l ON l.tour_id = t.id
LEFT JOIN tour_waypoint w ON w.tour_id = t.id
GROUP BY 
    t.id, t.user_id, t.name, t.description, t.transport_type, 
    t.distance_km, t.estimated_time_min, t.popularity, t.child_friendliness;

CREATE CAST (text AS transport_type) 
    WITH INOUT 
    AS IMPLICIT;

-- =============================================================
--  DEFAULT DATA: Add a system user for development
-- =============================================================
INSERT INTO app_user (id, username, email, password_hash)
VALUES ('00000000-0000-0000-0000-000000000000', 'system_user', 'system@example.com', 'no_hash_yet')
ON CONFLICT DO NOTHING;