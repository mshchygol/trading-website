//AuditLog
export interface AuditLogItem {
    snapshot: string
    timestamp: string
}

export interface SnapshotValue {
    snapshot: string
    timestamp: string
    index: number
}

export interface SnapshotData {
    data: {
        bids: [string, number][]
        asks: [string, number][]
    }
}

//Chart
export interface ChartDataItem {
    name: number;
    value: number;
}

//OrderBook
export interface OrderBookEntry {
    name: number;
    value: number;
}

export interface Quote {
    formattedAmount: string;
    btcAmount: number;
    success: boolean;
}

export interface WebSocketMessage {
    bids: [number, number][];
    asks: [number, number][];
    quote?: {
        btcAmount: number;
        eurCost: number;
        success: boolean;
    };
}