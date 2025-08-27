// AuditLog

/** Represents a single audit log entry with snapshot data and timestamp. */
export interface AuditLogItem {
    snapshot: string
    timestamp: string
}

/** Represents a snapshot with its parsed values and position in the list. */
export interface SnapshotValue {
    snapshot: string
    timestamp: string
    index: number
}

/** Represents the structure of a parsed snapshot, including bids and asks. */
export interface SnapshotData {
    data: {
        bids: [string, number][]
        asks: [string, number][]
    }
}

// Chart

/** Represents a single data point to be displayed in a chart. */
export interface ChartDataItem {
    name: number
    value: number
}

// OrderBook

/** Represents a single entry in the order book with price and amount. */
export interface OrderBookEntry {
    name: number
    value: number
}

/** Represents a quote calculation result with formatted values. */
export interface Quote {
    formattedAmount: string
    btcAmount: number
    success: boolean
}

/** Represents a WebSocket message containing order book updates and optional quote. */
export interface WebSocketMessage {
    bids: [number, number][]
    asks: [number, number][]
    quote?: {
        btcAmount: number
        eurCost: number
        success: boolean
    }
}
